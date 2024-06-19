using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Unity.Services.Core.Editor.Environments.Analytics;
using Unity.Services.Core.Editor.Shared.EditorUtils;
using UnityEditor;

namespace Unity.Services.Core.Editor.Environments
{
    sealed class EnvironmentService : IEnvironmentService, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public IReadOnlyCollection<EnvironmentInfo> Environments { get; private set; }

        public Guid? ActiveEnvironmentId
        {
            get
            {
                var environmentInfo = this.EnvironmentInfoFromName(m_EnvironmentProvider.ActiveEnvironmentName);
                if (environmentInfo != null && Guid.TryParse(environmentInfo.Value.Id.ToString(), out var envId))
                {
                    return envId;
                }
                return null;
            }
        }

        readonly IEnvironmentFetcher m_EnvironmentFetcher;
        readonly IEnvironmentProvider m_EnvironmentProvider;
        readonly IEnvironmentAnalytics m_EnvironmentAnalytics;
        readonly EditorValueTracker<string> m_ProjectIdTracker;

        Task m_RefreshTask;

        public EnvironmentService(
            IEnvironmentFetcher environmentFetcher,
            IEnvironmentProvider environmentProvider,
            IEnvironmentAnalytics environmentAnalytics)
        {
            m_EnvironmentFetcher = environmentFetcher;
            m_EnvironmentProvider = environmentProvider;
            m_EnvironmentAnalytics = environmentAnalytics;

            m_ProjectIdTracker = new EditorValueTracker<string>(() => CloudProjectSettings.projectId);
            m_ProjectIdTracker.ValueChanged += (_, __) =>
            {
                if (string.IsNullOrEmpty(CloudProjectSettings.projectId))
                {
                    return;
                }

                RefreshAsync();
            };
        }

        public Task RefreshAsync()
        {
            if (m_RefreshTask == null || m_RefreshTask.IsCompleted)
            {
                m_RefreshTask = RefreshInternal();
            }

            return m_RefreshTask;
        }

        public void SetActiveEnvironment(EnvironmentInfo environmentInfo)
        {
            SetActiveEnvironmentInternal(environmentInfo);
        }

        public void SetActiveEnvironment(string environmentName)
        {
            var environmentInfo = this.EnvironmentInfoFromName(environmentName);
            if (environmentInfo.HasValue)
            {
                SetActiveEnvironmentInternal(environmentInfo.Value);
            }
            else
            {
                throw new EnvironmentNotFoundException($"Could not find environment with name '{environmentName}'.");
            }
        }

        public void SetActiveEnvironment(Guid environmentGuid)
        {
            var environmentInfo = this.EnvironmentInfoFromId(environmentGuid);
            if (environmentInfo.HasValue)
            {
                SetActiveEnvironmentInternal(environmentInfo.Value);
            }
            else
            {
                throw new EnvironmentNotFoundException($"Could not find environment with guid '{environmentGuid}'.");
            }
        }

        void SetActiveEnvironmentInternal(EnvironmentInfo environment)
        {
            m_EnvironmentProvider.ActiveEnvironmentName = environment.Name;
            m_EnvironmentAnalytics.SendEnvironmentChangedEvent(ActiveEnvironmentId.ToString());
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveEnvironmentId)));
        }

        async Task RefreshInternal()
        {
            Environments = null;
            Environments = await m_EnvironmentFetcher.FetchEnvironments();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Environments)));
        }

        public void Dispose()
        {
            m_ProjectIdTracker.Dispose();
            m_RefreshTask?.Dispose();
        }
    }
}
