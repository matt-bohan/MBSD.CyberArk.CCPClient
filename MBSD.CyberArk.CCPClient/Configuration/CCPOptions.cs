using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace MBSD.CyberArk.CCPClient.Configuration
{
    /// <summary>
    /// Configuration options for CyberArk Central Credential Provider (CCP)
    /// </summary>
    public class CCPOptions
    {
        /// <summary>
        /// Base URL for the CyberArk CCP server (e.g., https://ccp.company.com)
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Default Application ID for CCP authentication (can be overridden per request)
        /// </summary>
        public string DefaultApplicationId { get; set; } = string.Empty;

        /// <summary>
        /// CCP endpoint path (default: /AIMWebService/api/Accounts)
        /// </summary>
        public string Endpoint { get; set; } = "/AIMWebService/api/Accounts";

        /// <summary>
        /// Connection timeout in seconds (default: 30)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to verify SSL certificates (default: true)
        /// </summary>
        public bool VerifySsl { get; set; } = true;

        /// <summary>
        /// Default client certificate configuration (can be overridden per request)
        /// </summary>
        public CertificateConfig DefaultCertificate { get; set; } = new CertificateConfig();

        /// <summary>
        /// Pre-configured certificates for different Application IDs
        /// </summary>
        public Dictionary<string, CertificateConfig> CertificatesByApplicationId { get; set; } = new Dictionary<string, CertificateConfig>();

        /// <summary>
        /// Validates the configuration options
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new ArgumentException("BaseUrl is required", nameof(BaseUrl));

            // Application ID is now optional at the client level since it can be provided per request
            
            // Validate default certificate if configured
            if (DefaultCertificate.IsConfigured)
                DefaultCertificate.Validate();

            // Validate all pre-configured certificates
            foreach (var cert in CertificatesByApplicationId.Values)
            {
                if (cert.IsConfigured)
                    cert.Validate();
            }
        }
    }

    /// <summary>
    /// Certificate configuration for client authentication
    /// </summary>
    public class CertificateConfig
    {
        /// <summary>
        /// Client certificate path for certificate-based authentication
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Client certificate password
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Client certificate thumbprint (for loading from certificate store)
        /// </summary>
        public string Thumbprint { get; set; } = string.Empty;

        /// <summary>
        /// Certificate store location (default: CurrentUser)
        /// </summary>
        public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;

        /// <summary>
        /// Certificate store name (default: My)
        /// </summary>
        public StoreName StoreName { get; set; } = StoreName.My;

        /// <summary>
        /// Indicates whether certificate authentication is configured
        /// </summary>
        public bool IsConfigured => 
            !string.IsNullOrWhiteSpace(FilePath) || 
            !string.IsNullOrWhiteSpace(Thumbprint);

        /// <summary>
        /// Validates the certificate configuration
        /// </summary>
        public void Validate()
        {
            if (!string.IsNullOrWhiteSpace(FilePath) && !string.IsNullOrWhiteSpace(Thumbprint))
                throw new ArgumentException("Cannot specify both FilePath and Thumbprint in certificate configuration");
        }

        /// <summary>
        /// Creates a certificate config from file path
        /// </summary>
        public static CertificateConfig FromFile(string filePath, string password = null) =>
            new CertificateConfig { FilePath = filePath, Password = password ?? string.Empty };

        /// <summary>
        /// Creates a certificate config from certificate store
        /// </summary>
        public static CertificateConfig FromStore(string thumbprint, StoreLocation storeLocation = StoreLocation.CurrentUser, StoreName storeName = StoreName.My) =>
            new CertificateConfig { Thumbprint = thumbprint, StoreLocation = storeLocation, StoreName = storeName };
    }
}