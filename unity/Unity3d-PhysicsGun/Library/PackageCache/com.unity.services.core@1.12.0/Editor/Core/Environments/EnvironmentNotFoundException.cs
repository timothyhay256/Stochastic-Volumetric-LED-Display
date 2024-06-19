using System;

namespace Unity.Services.Core.Editor.Environments
{
    /// <summary>
    /// The exception thrown when no environment is found.
    /// </summary>
    public class EnvironmentNotFoundException : Exception
    {
        /// <summary>
        /// Constructor for <see cref="EnvironmentNotFoundException"/>
        /// </summary>
        /// <param name="message"></param>
        public EnvironmentNotFoundException(string message)
            : base(message) {}
    }
}
