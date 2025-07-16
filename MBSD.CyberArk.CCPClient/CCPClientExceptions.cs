using System;
 

namespace MBSD.CyberArk.CCPClient
{
    /// <summary>
    /// Custom exception for CyberArk CCP errors
    /// </summary>
    public class CCPException : Exception
    {
        /// <summary>
        /// CyberArk error code if available
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// HTTP status code from the response
        /// </summary>
        public int HttpStatusCode { get; }

        /// <summary>
        /// Raw response content
        /// </summary>
        public string ResponseContent { get; }


        /// <summary>
        /// Application ID used in the failed request
        /// </summary>
        public string ApplicationId { get; }


        /// <summary>
        /// Initializes a new instance of the <see cref="CCPException"/> class with a specified error message, error
        /// code, HTTP status code, response content, and application ID.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The error code associated with the exception. This parameter is optional and defaults to an empty string.</param>
        /// <param name="httpStatusCode">The HTTP status code related to the exception. This parameter is optional and defaults to 0.</param>
        /// <param name="responseContent">The content of the response that caused the exception. This parameter is optional and defaults to an empty
        /// string.</param>
        /// <param name="applicationId">The application ID where the exception occurred. This parameter is optional and defaults to an empty string.</param>
        public CCPException(string message, string errorCode = "", int httpStatusCode = 0, string responseContent = "", string applicationId = "")
            : base(message)
        {
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
            ResponseContent = responseContent;
            ApplicationId = applicationId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CCPException"/> class with a specified error message, a
        /// reference to the inner exception that is the cause of this exception, and optional additional details.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is
        /// specified.</param>
        /// <param name="errorCode">An optional error code that identifies the specific error condition. Defaults to an empty string.</param>
        /// <param name="httpStatusCode">An optional HTTP status code associated with the error. Defaults to 0.</param>
        /// <param name="responseContent">Optional content of the response that may provide additional context for the error. Defaults to an empty
        /// string.</param>
        /// <param name="applicationId">An optional identifier for the application where the error occurred. Defaults to an empty string.</param>
        public CCPException(string message, Exception innerException, string errorCode = "", int httpStatusCode = 0, string responseContent = "", string applicationId = "")
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            HttpStatusCode = httpStatusCode;
            ResponseContent = responseContent;
            ApplicationId = applicationId;
        }
    }


}


