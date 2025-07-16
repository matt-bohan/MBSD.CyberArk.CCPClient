/*
 * Copyright 2024 Matthew Bohan
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * DISCLAIMER: This library is developed independently by Matthew Bohan and is not 
 * affiliated with, endorsed by, or sponsored by CyberArk / CyberArk Software Ltd.
 * CyberArk® and related names are property of CyberArk Software Ltd. or its affiliates.
 */

using MBSD.CyberArk.CCPClient.Configuration;
using MBSD.CyberArk.CCPClient.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Diagnostics.CodeAnalysis;

namespace MBSD.CyberArk.CCPClient
{
    /// <summary>
    /// CyberArk Central Credential Provider (CCP) client implementation
    /// </summary>


    [SuppressMessage("StyleCop.CSharp.NamingRules", "S101:Type name should comply with naming convention", 
        Justification = "CCPClient represents a CyberArk Central Credential Provider client so the capitalisation makes sense")]

    [SuppressMessage("Security", "S4830:Server certificate validation should not be disabled",
        Justification = "Certificate validation can be disabled through configuration for testing/development environments. A warning is generated at runtime.")]

    public class CCPClient : ICCPClient 
    {
        private readonly HttpClient _defaultHttpClient;
        private readonly CCPOptions _options;
        private readonly ILogger<CCPClient> _logger;
        private readonly ConcurrentDictionary<string, HttpClient> _certificateHttpClients;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the CcpClient
        /// </summary>
        /// <param name="httpClient">Default HTTP client (for non-certificate requests)</param>
        /// <param name="options">CCP configuration options</param>
        /// <param name="logger">Logger instance</param>
        public CCPClient(
            HttpClient httpClient,
            IOptions<CCPOptions> options,
            ILogger<CCPClient> logger = null)
        {
            _defaultHttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CCPClient>.Instance;
            _certificateHttpClients = new ConcurrentDictionary<string, HttpClient>();

            _options.Validate();
        }

        /// <summary>
        /// Retrieves a secret from CCP
        /// </summary>
        public async Task<CCPSecret> GetSecretAsync(SecretRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Object))
                throw new ArgumentException("Object name is required", nameof(request));

            var applicationId = GetEffectiveApplicationId(request);
            if (string.IsNullOrWhiteSpace(applicationId))
                throw new ArgumentException("Application ID must be specified either in request or in options.DefaultApplicationId");

            try
            {
                _logger.LogDebug("Retrieving secret for object: {ObjectName} using Application ID: {ApplicationId}", 
                    request.Object, applicationId);

                var httpClient = await GetHttpClientForRequestAsync(request);
                var queryString = BuildQueryString(request, applicationId);
                var requestUri = $"{_options.BaseUrl.TrimEnd('/')}{_options.Endpoint}?{queryString}";

                _logger.LogDebug("Making CCP request to: {RequestUri}", SanitizeUriForLogging(requestUri));

                var response = await httpClient.GetAsync(requestUri, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("CCP request failed with status {StatusCode}: {ErrorContent}", 
                        response.StatusCode, errorContent);
                    
                    throw new CCPException(
                        $"Failed to retrieve secret '{request.Object}' using Application ID '{applicationId}'. Status: {response.StatusCode}",
                        httpStatusCode: (int)response.StatusCode,
                        responseContent: errorContent,
                        applicationId: applicationId);
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("CCP response received successfully for object: {ObjectName}", request.Object);

                var ccpSecret = JsonConvert.DeserializeObject<CCPSecret>(jsonContent);
                
                if (ccpSecret == null)
                {
                    throw new CCPException($"Failed to deserialize CCP response for object '{request.Object}'", 
                        applicationId: applicationId);
                }

                _logger.LogInformation("Successfully retrieved secret for object: {ObjectName}, Safe: {Safe}, Application ID: {ApplicationId}", 
                    ccpSecret.Name, ccpSecret.Safe, applicationId);

                return ccpSecret;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request exception while retrieving secret for object: {ObjectName}", request.Object);
                throw new CCPException($"Network error while retrieving secret '{request.Object}'", ex, applicationId: applicationId);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                _logger.LogError(ex, "Request cancelled while retrieving secret for object: {ObjectName}", request.Object);
                throw new CCPException($"Request cancelled while retrieving secret '{request.Object}'", ex, applicationId: applicationId);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Request timeout while retrieving secret for object: {ObjectName}", request.Object);
                throw new CCPException($"Request timeout while retrieving secret '{request.Object}'", ex, applicationId: applicationId);
            }
        }

        /// <summary>
        /// Retrieves a secret by object name using default Application ID, SafeName and object name - which is the recommended efficient way of retrieving CCP secrets
        /// </summary>
        public async Task<CCPSecret> GetSecretAsync(string applicationId, string safeName, string objectName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Object name cannot be null or empty", nameof(objectName));

            var request = SecretRequest
                                       .ForObject(objectName)
                                       .UsingApplicationId(applicationId)
                                       .InSafe(safeName);
       

            return await GetSecretAsync(request, cancellationToken);
        }

        /// <summary>
        /// Retrieves only the password content by object name
        /// </summary>
        public async Task<string> GetPasswordOnlyAsync(string applicationId, string safeName, string objectName, CancellationToken cancellationToken = default)
        {
            var secret = await GetSecretAsync(applicationId, safeName, objectName, cancellationToken);
            return secret.Content;
        }

        /// <summary>
        /// Retrieves only the password content
        /// </summary>
        public async Task<string> GetPasswordOnlyAsync(SecretRequest request, CancellationToken cancellationToken = default)
        {
            var secret = await GetSecretAsync(request, cancellationToken);
            return secret.Content;
        }

        /// <summary>
        /// Tests the connection to CyberArk CCP using default Application ID
        /// </summary>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.DefaultApplicationId))
            {
                _logger.LogWarning("Cannot test connection: no default Application ID configured");
                return false;
            }

            try
            {
                _logger.LogDebug("Testing connection to CyberArk CCP");

                var testUri = $"{_options.BaseUrl.TrimEnd('/')}{_options.Endpoint}?AppID={HttpUtility.UrlEncode(_options.DefaultApplicationId)}&Object=test";
                
                var response = await _defaultHttpClient.GetAsync(testUri, cancellationToken);
                
                var isConnected = response.StatusCode != System.Net.HttpStatusCode.ServiceUnavailable &&
                                response.StatusCode != System.Net.HttpStatusCode.BadGateway &&
                                response.StatusCode != System.Net.HttpStatusCode.GatewayTimeout;

                _logger.LogDebug("Connection test result: {IsConnected} (Status: {StatusCode})", 
                    isConnected, response.StatusCode);
                
                return isConnected;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Connection test failed due to network error");
                return false;
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                _logger.LogError(ex, "Connection test cancelled");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "Connection test timeout");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during connection test");
                return false;
            }
        }

        #region Synchronous Methods

        /// <summary>
        /// Retrieves a secret from CCP (synchronous)
        /// </summary>
        public CCPSecret GetSecret(SecretRequest request)
        {
            return GetSecretAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves a secret by application id, safe name and object name (the recommended efficient way of retrieving CCP secrets  (synchronous) 
        /// </summary>
        public CCPSecret GetSecret(string applicationId, string safeName, string objectName)
        {
            return GetSecretAsync(applicationId, safeName, objectName).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves only the password content by object name (synchronous)
        /// </summary>
        public string GetPasswordOnly(string applicationId, string safeName, string objectName)
        {
            return GetPasswordOnlyAsync(applicationId, safeName, objectName).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves only the password content (synchronous)
        /// </summary>
        public string GetPasswordOnly(SecretRequest request)
        {
            return GetPasswordOnlyAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
        }

       

        /// <summary>
        /// Tests the connection to CyberArk CCP (synchronous)
        /// </summary>
        public bool TestConnection()
        {
            return TestConnectionAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        #endregion

        /// <summary>
        /// Gets the effective Application ID for a request
        /// </summary>
        private string GetEffectiveApplicationId(SecretRequest request)
        {
            return !string.IsNullOrWhiteSpace(request.ApplicationId) 
                ? request.ApplicationId 
                : _options.DefaultApplicationId;
        }

        /// <summary>
        /// Gets the effective certificate configuration for a request
        /// </summary>
        private CertificateConfig GetEffectiveCertificateConfig(SecretRequest request)
        {
            // 1. Request-specific certificate takes precedence
            if (request.Certificate?.IsConfigured == true)
                return request.Certificate;

            // 2. Application ID-specific certificate
            var applicationId = GetEffectiveApplicationId(request);
            if (!string.IsNullOrWhiteSpace(applicationId) && 
                _options.CertificatesByApplicationId.TryGetValue(applicationId, out var appIdCert) &&
                appIdCert.IsConfigured)
                return appIdCert;

            // 3. Default certificate
            if (_options.DefaultCertificate.IsConfigured)
                return _options.DefaultCertificate;

            return null;
        }

        /// <summary>
        /// Gets the appropriate HTTP client for the request (with certificate if needed)
        /// </summary>
        private Task<HttpClient> GetHttpClientForRequestAsync(SecretRequest request)
        {
            var certificateConfig = GetEffectiveCertificateConfig(request);
            
            if (certificateConfig == null)
            {
                return Task.FromResult(_defaultHttpClient);
            }

            var cacheKey =  GetCertificateCacheKey(certificateConfig);

            return Task.FromResult(_certificateHttpClients.GetOrAdd(cacheKey, _ => CreateHttpClientWithCertificate(certificateConfig)));
        }

        /// <summary>
        /// Creates a cache key for certificate configuration
        /// </summary>
        private static string GetCertificateCacheKey(CertificateConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.FilePath))
                return $"file:{config.FilePath}:{config.Password}";
            
            return $"store:{config.Thumbprint}:{config.StoreLocation}:{config.StoreName}";
        }

        /// <summary>
        /// Creates an HTTP client with the specified certificate
        /// </summary>
        private HttpClient CreateHttpClientWithCertificate(CertificateConfig certificateConfig)
        {
            var handler = new HttpClientHandler();
            
            if (!_options.VerifySsl)
            {
                handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                _logger.LogWarning("SSL certificate verification is disabled. This exposes you to security risks and is not recommended for use in production.");
            }

            try
            {
                var certificate = LoadCertificate(certificateConfig);
                handler.ClientCertificates.Add(certificate);
                
                _logger.LogDebug("Created HTTP client with certificate: {CertificateSubject}", certificate.Subject);
            }
            catch (Exception ex)
            {
                handler.Dispose();
                throw new CCPException($"Failed to load certificate: {ex.Message}", ex);
            }

            var client = new HttpClient(handler);
            client.BaseAddress = new Uri(_options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("User-Agent", "MBSD-CyberArk-CCPClient/1.0.0");
            
            return client;
        }

        /// <summary>
        /// Loads a certificate based on configuration
        /// </summary>
        private static X509Certificate2 LoadCertificate(CertificateConfig config)
        {
            if (!string.IsNullOrWhiteSpace(config.FilePath))
            {
                return !string.IsNullOrWhiteSpace(config.Password)
                    ? new X509Certificate2(config.FilePath, config.Password)
                    : new X509Certificate2(config.FilePath);
            }

            if (!string.IsNullOrWhiteSpace(config.Thumbprint))
            {
                X509Store store = new X509Store(config.StoreName, config.StoreLocation);
                try
                {
                    store.Open(OpenFlags.ReadOnly);

                    var certificates = store.Certificates.Find(X509FindType.FindByThumbprint, config.Thumbprint, false);

                    if (certificates.Count == 0)
                    {
                        throw new CCPException(
                            $"Certificate with thumbprint '{config.Thumbprint}' not found in store '{config.StoreName}' at location '{config.StoreLocation}'");
                    }

                    return certificates[0];
                }
                finally
                {
                    store.Dispose();
                }
            }

            throw new CCPException("Invalid certificate configuration");
        }

        /// <summary>
        /// Builds the query string for CCP requests
        /// </summary>
        private static string BuildQueryString(SecretRequest request, string applicationId)
        {
            var parameters = new List<string>
            {
                $"AppID={HttpUtility.UrlEncode(applicationId)}",
                $"Object={HttpUtility.UrlEncode(request.Object)}"
            };

            if (!string.IsNullOrWhiteSpace(request.Safe))
                parameters.Add($"Safe={HttpUtility.UrlEncode(request.Safe)}");

            if (!string.IsNullOrWhiteSpace(request.Folder))
                parameters.Add($"Folder={HttpUtility.UrlEncode(request.Folder)}");

            if (!string.IsNullOrWhiteSpace(request.UserName))
                parameters.Add($"UserName={HttpUtility.UrlEncode(request.UserName)}");

            if (!string.IsNullOrWhiteSpace(request.Address))
                parameters.Add($"Address={HttpUtility.UrlEncode(request.Address)}");

            if (!string.IsNullOrWhiteSpace(request.Database))
                parameters.Add($"Database={HttpUtility.UrlEncode(request.Database)}");

            if (!string.IsNullOrWhiteSpace(request.PolicyID))
                parameters.Add($"PolicyID={HttpUtility.UrlEncode(request.PolicyID)}");

            foreach (var param in request.CustomParameters)
            {
                if (!string.IsNullOrWhiteSpace(param.Key) && param.Value != null)
                {
                    parameters.Add($"{HttpUtility.UrlEncode(param.Key)}={HttpUtility.UrlEncode(param.Value)}");
                }
            }

            return string.Join("&", parameters);
        }

        /// <summary>
        /// Sanitizes URI for logging by removing sensitive parameters
        /// </summary>
        private static string SanitizeUriForLogging(string uri)
        {
            try
            {
                var uriObj = new Uri(uri);
                return $"{uriObj.Scheme}://{uriObj.Host}{uriObj.PathAndQuery.Split('?')[0]}?[QUERY_PARAMETERS]";
            }
            catch
            {
                return "[Invalid URI]";
            }
        }



        

        /// <summary>
        /// Disposes the client and all cached HTTP clients
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                foreach (var client in _certificateHttpClients.Values)
                {
                    try
                    {
                        client?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing certificate HTTP client");
                    }
                }
                
                _certificateHttpClients.Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes the CCPClient instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


    }
}