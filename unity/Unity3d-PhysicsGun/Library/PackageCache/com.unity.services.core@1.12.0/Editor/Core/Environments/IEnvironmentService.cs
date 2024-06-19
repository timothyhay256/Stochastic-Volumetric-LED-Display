using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Unity.Services.Core.Editor.Environments
{
    interface IEnvironmentService : INotifyPropertyChanged
    {
        IReadOnlyCollection<EnvironmentInfo> Environments { get; }
        Guid? ActiveEnvironmentId { get; }
        Task RefreshAsync();
        void SetActiveEnvironment(EnvironmentInfo environmentInfo);
        void SetActiveEnvironment(string environmentName);
        void SetActiveEnvironment(Guid environmentGuid);
    }
}
