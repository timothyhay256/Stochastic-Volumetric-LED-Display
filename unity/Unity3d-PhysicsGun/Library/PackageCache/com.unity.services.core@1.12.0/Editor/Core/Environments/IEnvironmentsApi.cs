using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Unity.Services.Core.Editor.Environments
{
    /// <summary>
    /// Interface to access environment information and validation.
    /// </summary>
    public interface IEnvironmentsApi : INotifyPropertyChanged
    {
        /// <summary>
        /// The name for the active environment.
        /// </summary>
        string ActiveEnvironmentName { get; }

        /// <summary>
        /// The environments currently available.
        /// </summary>
        IReadOnlyCollection<EnvironmentInfo> Environments { get; }

        /// <summary>
        /// The guid for the active environment.
        /// </summary>
        Guid? ActiveEnvironmentId { get; }

        /// <summary>
        /// Refreshes the list of environments.
        /// </summary>
        /// <returns>The <see cref="Task"/> for the refresh.</returns>
        Task RefreshAsync();

        /// <summary>
        /// Set the active environment.
        /// </summary>
        /// <param name="environmentInfo">The <see cref="EnvironmentInfo"/> to set as active</param>
        void SetActiveEnvironment(EnvironmentInfo environmentInfo);

        /// <summary>
        /// Set the active environment.
        /// </summary>
        /// <param name="environmentName">The name of the environment to set as active</param>
        void SetActiveEnvironment(string environmentName);

        /// <summary>
        /// Set the active environment.
        /// </summary>
        /// <param name="environmentGuid">The guid of the environment to set as active</param>
        void SetActiveEnvironment(Guid environmentGuid);

        /// <summary>
        /// Validates that the user is logged in, their project is linked,
        /// the environment has been selected, and the environment is valid for the project.
        /// </summary>
        /// <returns>The <see cref="ValidationResult"/> of the validation.</returns>
        Task<ValidationResult> ValidateEnvironmentAsync();
    }
}
