namespace Unity.Services.Core.Editor.Environments
{
    /// <summary>
    /// The result of validating an environment.
    /// </summary>
    public sealed class ValidationResult
    {
        /// <summary>
        /// If the validation failed.
        /// </summary>
        public bool Failed => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// The error that caused the failure.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// ValidationResult Constructor.
        /// </summary>
        public ValidationResult()
        {
            ErrorMessage = null;
        }

        /// <summary>
        /// ValidationResult Constructor.
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        public ValidationResult(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}
