using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Unity.Services.Core.Editor.Environments
{
    class EnvironmentsApiInternal : IEnvironmentsApi
    {
        public IReadOnlyCollection<EnvironmentInfo> Environments => m_EnvironmentService.Environments;
        public string ActiveEnvironmentName => m_EnvironmentProvider.ActiveEnvironmentName;
        public Guid? ActiveEnvironmentId => m_EnvironmentService.ActiveEnvironmentId;
        public event PropertyChangedEventHandler PropertyChanged;

        readonly IEnvironmentProvider m_EnvironmentProvider;
        readonly IEnvironmentService m_EnvironmentService;
        readonly IEnvironmentValidator m_EnvironmentValidator;

        internal EnvironmentsApiInternal(
            IEnvironmentProvider environmentProvider,
            IEnvironmentService environmentService,
            IEnvironmentValidator environmentValidator)
        {
            m_EnvironmentProvider = environmentProvider;
            m_EnvironmentService = environmentService;
            m_EnvironmentValidator = environmentValidator;

            m_EnvironmentService.PropertyChanged += OnEnvironmentPropertyChanged;
            m_EnvironmentProvider.PropertyChanged += OnEnvironmentPropertyChanged;
        }

        public Task RefreshAsync()
        {
            return m_EnvironmentService.RefreshAsync();
        }

        public void SetActiveEnvironment(EnvironmentInfo environmentInfo)
        {
            m_EnvironmentService.SetActiveEnvironment(environmentInfo);
        }

        public void SetActiveEnvironment(string environmentName)
        {
            m_EnvironmentService.SetActiveEnvironment(environmentName);
        }

        public void SetActiveEnvironment(Guid environmentGuid)
        {
            m_EnvironmentService.SetActiveEnvironment(environmentGuid);
        }

        public Task<ValidationResult> ValidateEnvironmentAsync()
        {
            return m_EnvironmentValidator.ValidateEnvironmentAsync();
        }

        void OnEnvironmentPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (PropertyChanged == null)
            {
                return;
            }

            switch (args.PropertyName)
            {
                case nameof(m_EnvironmentService.ActiveEnvironmentId):
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveEnvironmentId)));
                    break;
                case nameof(m_EnvironmentService.Environments):
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(Environments)));
                    break;
                case nameof(m_EnvironmentProvider.ActiveEnvironmentName):
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveEnvironmentName)));
                    break;
            }
        }
    }
}
