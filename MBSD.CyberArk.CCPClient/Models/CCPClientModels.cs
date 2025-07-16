using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using MBSD.CyberArk.CCPClient.Configuration;

namespace MBSD.CyberArk.CCPClient.Models
{
    /// <summary>
    /// Request model for retrieving secrets from CCP
    /// </summary>
    public class SecretRequest
    {
        /// <summary>
        /// Application ID for this specific request (overrides default if specified)
        /// </summary>
        public string ApplicationId { get; set; } = string.Empty;

        /// <summary>
        /// Certificate configuration for this specific request (overrides default if specified)
        /// </summary>
        public CertificateConfig Certificate { get; set; }

        /// <summary>
        /// The object name (account name) to retrieve
        /// </summary>
        public string Object { get; set; } = string.Empty;

        /// <summary>
        /// The safe name where the secret is stored
        /// </summary>
        public string Safe { get; set; } = string.Empty;

        /// <summary>
        /// The folder name within the safe
        /// </summary>
        public string Folder { get; set; } = string.Empty;

        /// <summary>
        /// Username to search for
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// Address/server to search for
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// Database name to search for
        /// </summary>
        public string Database { get; set; } = string.Empty;

        /// <summary>
        /// Policy ID to search for
        /// </summary>
        public string PolicyID { get; set; } = string.Empty;

        /// <summary>
        /// Additional custom query parameters
        /// </summary>
        public Dictionary<string, string> CustomParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a simple request with just an object name
        /// </summary>
        public static SecretRequest ForObject(string objectName) => new SecretRequest { Object = objectName };

        /// <summary>
        /// Creates a request for an object with specific Application ID
        /// </summary>
        public static SecretRequest ForObjectWithAppId(string objectName, string applicationId) => 
            new SecretRequest { Object = objectName, ApplicationId = applicationId };

        /// <summary>
        /// Creates a request for an object in a specific safe
        /// </summary>
        public static SecretRequest ForObjectInSafe(string objectName, string safeName) => 
            new SecretRequest { Object = objectName, Safe = safeName };


        /// <summary>
        /// Creates a request for an object in a specific safe and application ID - this is the recommended way of calling Central Credential Provider
        /// </summary>
        public static SecretRequest ForObjectInSafe(string applicationId, string safeName, string objectName) =>
            new SecretRequest { ApplicationId = applicationId, Safe = safeName, Object = objectName  };



        /// <summary>
        /// Creates a request with specific Application ID and certificate
        /// </summary>
        public static SecretRequest WithCertificate(string applicationId, string safeName, string objectName,   CertificateConfig certificate = null) =>
            new SecretRequest 
            { 
                Object = objectName, 
                ApplicationId = applicationId,
                Safe = safeName,
                Certificate = certificate
            };

        /// <summary>
        /// Sets the Application ID for this request
        /// </summary>
        public SecretRequest UsingApplicationId(string applicationId)
        {
            ApplicationId = applicationId;
            return this;
        }

        /// <summary>
        /// Sets the certificate for this request
        /// </summary>
        public SecretRequest UsingCertificate(CertificateConfig certificate)
        {
            Certificate = certificate;
            return this;
        }

        /// <summary>
        /// Sets certificate from file for this request
        /// </summary>
        public SecretRequest UsingCertificateFile(string filePath, string password = null)
        {
            Certificate = CertificateConfig.FromFile(filePath, password);
            return this;
        }

        /// <summary>
        /// Sets certificate from store for this request
        /// </summary>
        public SecretRequest UsingCertificateStore(string thumbprint, StoreLocation storeLocation = StoreLocation.CurrentUser, StoreName storeName = StoreName.My)
        {
            Certificate = CertificateConfig.FromStore(thumbprint, storeLocation, storeName);
            return this;
        }

        /// <summary>
        /// Sets the safe for this request
        /// </summary>
        public SecretRequest InSafe(string safeName)
        {
            Safe = safeName;
            return this;
        }

      

        /// <summary>
        /// Sets the folder for this request
        /// </summary>
        public SecretRequest InFolder(string folderName)
        {
            Folder = folderName;
            return this;
        }
    }

    /// <summary>
    /// Response model for CCP API calls
    /// </summary>
    public class CCPSecret
    {
        /// <summary>
        /// The retrieved content (password/secret)
        /// </summary>
        [JsonProperty("Content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// The username associated with the account
        /// </summary>
        [JsonProperty("UserName")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The address/server associated with the account
        /// </summary>
        [JsonProperty("Address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// The database name (if applicable)
        /// </summary>
        [JsonProperty("Database")]
        public string Database { get; set; } = string.Empty;

        /// <summary>
        /// The platform ID
        /// </summary>
        [JsonProperty("PlatformID")]
        public string PlatformId { get; set; } = string.Empty;

        /// <summary>
        /// The safe name
        /// </summary>
        [JsonProperty("Safe")]
        public string Safe { get; set; } = string.Empty;

        /// <summary>
        /// The folder name
        /// </summary>
        [JsonProperty("Folder")]
        public string Folder { get; set; } = string.Empty;

        /// <summary>
        /// The account name
        /// </summary>
        [JsonProperty("Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The policy ID
        /// </summary>
        [JsonProperty("PolicyID")]
        public string PolicyId { get; set; } = string.Empty;

        /// <summary>
        /// When the password was last changed by CPM
        /// </summary>
        [JsonProperty("CPMLastChangeTime")]
        public DateTime? LastChangeTime { get; set; }

        /// <summary>
        /// When the password will be changed next by CPM
        /// </summary>
        [JsonProperty("CPMNextChangeTime")]
        public DateTime? NextChangeTime { get; set; }

        /// <summary>
        /// The creation method of the account
        /// </summary>
        [JsonProperty("CreationMethod")]
        public string CreationMethod { get; set; } = string.Empty;

        /// <summary>
        /// Additional custom properties returned by CCP
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the password/secret content
        /// </summary>
        public string Password => Content;
    }

    
}