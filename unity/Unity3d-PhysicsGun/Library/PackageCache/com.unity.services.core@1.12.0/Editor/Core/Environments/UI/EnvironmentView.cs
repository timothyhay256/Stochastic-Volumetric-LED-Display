using System;
using System.Linq;
using Unity.Services.Core.Editor.Shared.UI;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Services.Core.Editor.Environments.UI
{
    /// <summary>
    /// The <see cref="VisualElement"/> that displays the currently selected environment
    /// </summary>
    public class EnvironmentView : VisualElement
    {
        const string k_TemplatePath = "Packages/com.unity.services.core/Editor/Core/Environments/UI/Assets/EnvironmentView.uxml";
        const string k_StylePath = "Packages/com.unity.services.core/Editor/Core/Environments/UI/Assets/EnvironmentView_style.uss";

        static readonly string k_EnvironmentSettingsMenuItemText = L10n.Tr("Environment Settings");
        static readonly string k_EnvironmentNotSet = L10n.Tr("Environment not set");
        const string k_IconClass = "environment-icon";
        internal const string WarningClass = "warning";

        readonly ModelBinding<IEnvironmentsApi> m_EnvironmentBinding;

        /// <summary>
        /// Constructor for <see cref="EnvironmentView"/>
        /// </summary>
        public EnvironmentView()
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_TemplatePath);
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath));

            visualTreeAsset.CloneTree(this);
            var toolbarMenu = this.Q<ToolbarMenu>();
            toolbarMenu.menu.AppendAction(
                k_EnvironmentSettingsMenuItemText,
                _ => OpenEnvironmentSettings());
            var icon = new VisualElement();
            icon.AddToClassList(k_IconClass);
            toolbarMenu.Insert(0, icon);

            m_EnvironmentBinding = new ModelBinding<IEnvironmentsApi>(this);
            m_EnvironmentBinding.BindProperty(nameof(IEnvironmentsApi.Environments), value =>
            {
                UpdateBindings(value, false);
            });
            m_EnvironmentBinding.BindProperty(nameof(IEnvironmentsApi.ActiveEnvironmentId), value =>
            {
                UpdateBindings(value, true);
            });
        }

        /// <summary>
        /// Bind the environment service
        /// </summary>
        /// <param name="environmentService">The service</param>
        public void Bind(IEnvironmentsApi environmentService)
        {
            m_EnvironmentBinding.Source = environmentService;
        }

        void UpdateBindings(IEnvironmentsApi service, bool shouldRequestRequery = false)
        {
            var menu = this.Q<ToolbarMenu>();

            var activeEnvironment = EnvironmentInfoFromId(service.ActiveEnvironmentId);

            var validEnv = activeEnvironment != null;
            menu.text = validEnv ? activeEnvironment.Value.Name : k_EnvironmentNotSet;
            if (!validEnv && shouldRequestRequery)
            {
                service.RefreshAsync();
            }

            if (activeEnvironment?.IsDefault ?? false)
            {
                AddToClassList(WarningClass);
            }
            else
            {
                RemoveFromClassList(WarningClass);
            }
        }

        EnvironmentInfo? EnvironmentInfoFromId(Guid? environmentId)
        {
            if (environmentId == null ||
                m_EnvironmentBinding.Source.Environments == null)
            {
                return null;
            }

            return m_EnvironmentBinding.Source.Environments
                .Cast<EnvironmentInfo?>() // ensure FirstOrDefault will return null instead of default(EnvironmentInfo)
                .FirstOrDefault(info => info != null && info.Value.Id == environmentId);
        }

        internal static EditorWindow OpenEnvironmentSettings()
        {
            return SettingsService.OpenProjectSettings(EnvironmentsConstants.SettingsLocation);
        }

        new class UxmlFactory : UxmlFactory<EnvironmentView> {} // NOSONAR
    }
}
