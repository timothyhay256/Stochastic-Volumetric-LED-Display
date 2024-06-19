using System;
using System.IO;
using System.Linq;
using Unity.Services.Core.Editor.Shared.EditorUtils;
using Unity.Services.Core.Editor.Shared.UI;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Services.Core.Editor.Environments.UI
{
    class EnvironmentSelector : VisualElement
    {
        const string k_UxmlPath = "Packages/com.unity.services.core/Editor/Core/Environments/UI/Assets/EnvironmentSelectorUI.uxml";
#if UNITY_2021_3_OR_NEWER
        const string k_UxmlPathDropdown = "Packages/com.unity.services.core/Editor/Core/Environments/UI/Assets/EnvironmentDropdown.uxml";
#endif

        readonly ModelBinding<IEnvironmentService> m_EnvironmentBindings;

        VisualElement m_ContainerDropdown;
        VisualElement m_ContainerFetching;
        VisualElement m_ContainerWarning;

#if UNITY_2021_3_OR_NEWER
        DropdownField m_DropdownControl;
#else
        PopupField<string> m_DropdownControl;
#endif


        public EnvironmentSelector()
        {
            m_EnvironmentBindings = new ModelBinding<IEnvironmentService>(this);
            m_EnvironmentBindings.BindProperty(nameof(IEnvironmentService.Environments), service =>
            {
                if (service.Environments == null)
                {
                    SetVisibleContainer(m_ContainerFetching);
                }
                else if (m_DropdownControl != null)
                {
                    m_DropdownControl.choices = service.Environments.Select(env => env.Name).ToList();

                    var currentEnvInfo = service.ActiveEnvironmentInfo();
                    if (currentEnvInfo != null)
                    {
                        m_DropdownControl.SetValueWithoutNotify(currentEnvInfo.Value.Name);
                    }

                    SetVisibleContainer(m_ContainerDropdown);
                }
                else
                {
                    throw new Exception("Dropdown field of the Environment Selector has not been set.");
                }

                OnEnvironmentChanged(service.ActiveEnvironmentInfo());
            });
            m_EnvironmentBindings.BindProperty(nameof(IEnvironmentService.ActiveEnvironmentId), service =>
            {
                OnEnvironmentChanged(service.ActiveEnvironmentInfo());
            });
        }

        public void Bind(IEnvironmentService environmentService)
        {
            m_EnvironmentBindings.Source = environmentService;

            Setup();

            m_DropdownControl.RegisterValueChangedCallback(v =>
            {
                var info = environmentService.EnvironmentInfoFromName(v.newValue);
                if (info != null)
                {
                    environmentService.SetActiveEnvironment(info.Value);
                }
            });

            Sync.SafeAsync(async() =>
            {
                await environmentService.RefreshAsync();
            });
        }

        void Setup()
        {
            LoadUxml(this);
            SetupDropdown(this);
            SetupManageEnvironments(this);
            SetupWarning(this);
        }

        static void LoadUxml(VisualElement containerElement)
        {
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            if (uxmlAsset != null)
            {
                uxmlAsset.CloneTree(containerElement);
            }
            else
            {
                throw new MissingReferenceException("Could not find a uxml asset to load.");
            }
        }

        void SetupDropdown(VisualElement containerElement)
        {
            m_ContainerDropdown = containerElement.Q(UxmlNames.ContainerDropdown);
            m_ContainerFetching = containerElement.Q(UxmlNames.ContainerFetching);

#if UNITY_2021_3_OR_NEWER
            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPathDropdown);
            if (uxmlAsset != null)
            {
                uxmlAsset.CloneTree(m_ContainerDropdown);
                m_DropdownControl = this.Q<DropdownField>();
            }
            else
            {
                throw new MissingReferenceException("Could not find a uxml asset to load.");
            }
#else
            m_DropdownControl = new PopupField<string> { name = "Dropdown Control" };
            m_ContainerDropdown.Add(m_DropdownControl);
            m_DropdownControl.label = "Editor Environment";
            m_DropdownControl.AddToClassList("no-margin");
#endif
        }

        static void SetupManageEnvironments(VisualElement containerElement)
        {
            var containerManageEnvironments = containerElement.Q(UxmlNames.ContainerManageEnvironments);
#if ENABLE_EDITOR_GAME_SERVICES
            var clickable = new Clickable(() =>
            {
                Application.OpenURL($"https://dashboard.unity3d.com/organizations/{CloudProjectSettings.organizationKey}/projects/{CloudProjectSettings.projectId}/settings/environments");
            });
            containerManageEnvironments.AddManipulator(clickable);
#else
            containerManageEnvironments.style.display = DisplayStyle.None;
#endif
        }

        void SetupWarning(VisualElement containerElement)
        {
            m_ContainerWarning = containerElement.Q(UxmlNames.ContainerWarning);
            m_ContainerWarning.style.display = DisplayStyle.None;
        }

        void OnEnvironmentChanged(EnvironmentInfo? environmentInfo)
        {
            m_ContainerWarning.style.display = environmentInfo?.IsDefault ?? false
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        void SetVisibleContainer(VisualElement containerElement)
        {
            m_ContainerDropdown.style.display =
                containerElement == m_ContainerDropdown
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            m_ContainerFetching.style.display =
                containerElement == m_ContainerFetching
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        static class UxmlNames
        {
            public const string ContainerDropdown = "Dropdown Section";
            public const string ContainerFetching = "Fetching Environments Section";
            public const string ContainerManageEnvironments = "Manage Environments Container";
            public const string ContainerWarning = "Default Environment Section";
        }
    }
}
