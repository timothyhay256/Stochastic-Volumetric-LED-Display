using System.Collections.Generic;
using Unity.Services.Core.Editor.Environments;
using Unity.Services.Core.Editor.Environments.UI;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Services.Core.Editor.Settings
{
    /// <summary>
    /// The class which provides the page in Project Settings for the Environments.
    /// </summary>
    class EnvironmentSettingsProvider : EditorGameServiceSettingsProvider
    {
        /// <summary>
        /// The <see cref="IEditorGameService"/> associated with this Settings Provider.
        /// </summary>
        protected override IEditorGameService EditorGameService
            => EditorGameServiceRegistry.Instance.GetEditorGameService<EnvironmentIdentifier>();

        /// <summary>
        /// The title.
        /// </summary>
        protected override string Title { get; } = EnvironmentsConstants.ServiceName;

        /// <summary>
        /// The description.
        /// </summary>
        protected override string Description { get; } = "Move assets and configurations to backend services from within the editor";

        readonly IEnvironmentService m_EnvironmentService;

        EnvironmentSettingsProvider(
            IEnvironmentService environmentService,
            string path,
            SettingsScope scopes,
            IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
            m_EnvironmentService = environmentService;
        }

        [SettingsProvider]
        static SettingsProvider CreateEnvironmentsSettingsProvider()
        {
            return new EnvironmentSettingsProvider(
                EnvironmentServiceRegistry.Instance.GetService<IEnvironmentService>(),
                GenerateProjectSettingsPath(EnvironmentsConstants.ServiceName),
                SettingsScope.Project);
        }

        /// <summary>
        /// The generator for the UI.
        /// </summary>
        /// <returns>The visual element added to the Settings Project page.</returns>
        protected override VisualElement GenerateServiceDetailUI()
        {
            var environmentSelector = new EnvironmentSelector();
            environmentSelector.Bind(m_EnvironmentService);
            return environmentSelector;
        }

        /// <summary>
        /// The generator for the UI when unsupported.
        /// </summary>
        /// <returns>The visual element added to the Settings Project page when unsupported.</returns>
        protected override VisualElement GenerateUnsupportedDetailUI()
        {
            return GenerateServiceDetailUI();
        }
    }
}
