using System.ComponentModel;

namespace Unity.Services.Core.Editor.Environments
{
    /// <summary>
    /// This interface represents a container for the current selected environment.
    /// </summary>
    interface IEnvironmentProvider : INotifyPropertyChanged
    {
        /// <summary>
        /// Environment Id of a currently selected environment.
        /// </summary>
        string ActiveEnvironmentName { get; set; }
    }
}
