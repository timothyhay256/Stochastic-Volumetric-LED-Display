using System;

namespace Unity.Services.Core
{
    /// <summary>
    /// Represents an error during services initialization
    /// </summary>
    public sealed class ServicesCreationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicesCreationException" /> class.
        /// </summary>
        /// <param name="message">
        /// The error message that explains the reason for the exception.
        /// </param>
        internal ServicesCreationException(string message)
            : base(message) {}
    }
}
