using MBSD.CyberArk.CCPClient.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MBSD.CyberArk.CCPClient
{
    /// <summary>
    /// Interface for CyberArk Central Credential Provider (CCP) client
    /// </summary>
    public interface ICCPClient : IDisposable
    {
        /// <summary>
        /// Retrieves a secret from CCP
        /// </summary>
        /// <param name="request">The secret request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The secret details</returns>
        Task<CCPSecret> GetSecretAsync(SecretRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a secret by object name
        /// </summary>
        /// <param name="objectName">The object name (account name)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="applicationId">The CyberArk application ID</param>
        /// <param name="safeName">The CyberArk Safe Name where the account/object is stored.</param>
        /// <returns>The secret details</returns>
        Task<CCPSecret> GetSecretAsync(string applicationId, string safeName, string objectName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves only the password/content from a secret
        /// </summary>
        /// <param name="applicationId">The CyberArk application ID</param>
        /// <param name="safeName">The CyberArk Safe Name where the account/object is stored.</param>
        /// <param name="objectName">The object name (account name)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The password/secret content</returns>
        Task<string> GetPasswordOnlyAsync(string applicationId, string safeName, string objectName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves only the password/content from a secret
        /// </summary>
        /// <param name="request">The secret request parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The password/secret content</returns>
        Task<string> GetPasswordOnlyAsync(SecretRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to CyberArk CCP
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        // Synchronous methods
        
        /// <summary>
        /// Retrieves a secret from CCP (synchronous)
        /// </summary>
        /// <param name="request">The secret request parameters</param>
        /// <returns>The secret details</returns>
        CCPSecret GetSecret(SecretRequest request);

        /// <summary>
        /// Retrieves a secret by object name (synchronous)
        /// </summary>
        /// <param name="applicationId">The application Id</param>
        /// <param name="safeName">The name of the safe the object/account is stored</param>
        /// <param name="objectName">The object name (account name)</param>

        /// <returns>The secret details</returns>
        CCPSecret GetSecret(string applicationId, string safeName, string objectName);

        /// <summary>
        /// Retrieves only the password/content from a secret (synchronous)
        /// </summary>
        /// <param name="applicationId">The application Id</param>
        /// <param name="safeName">The name of the safe the object/account is stored</param>
        /// <param name="objectName">The object name (account name)</param>
        /// <returns>The password/secret content</returns>
        string GetPasswordOnly(string applicationId, string safeName, string objectName);

        /// <summary>
        /// Retrieves only the password/content from a secret (synchronous)
        /// </summary>
        /// <param name="request">The secret request parameters</param>
        /// <returns>The password/secret content</returns>
        string GetPasswordOnly(SecretRequest request);

        /// <summary>
        /// Tests the connection to CyberArk CCP (synchronous)
        /// </summary>
        /// <returns>True if connection is successful</returns>
        bool TestConnection();
    }
}