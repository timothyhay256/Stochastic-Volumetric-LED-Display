using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// Popup Field for selecting the Active Target for Grid Painting.
    /// </summary>
    public sealed class TilePaletteActiveTargetsPopup : PopupField<GameObject>
    {
        private static string k_NullGameObjectName = L10n.Tr("No Valid Target");

        private static string k_LabelTooltip =
            L10n.Tr("Specifies the currently active Tilemap used for painting in the Scene View.");

        private static string k_WarningTooltip =
            L10n.Tr("Editing Tilemaps in Prefabs will have better performance if edited in Prefab Mode.");

        /// <summary>
        /// Factory for TilePaletteActiveTargetsPopup.
        /// </summary>
        public class TilePaletteActiveTargetsPopupFactory : UxmlFactory<TilePaletteActiveTargetsPopup, TilePaletteActiveTargetsPopupUxmlTraits> {}
        /// <summary>
        /// UxmlTraits for TilePaletteActiveTargetsPopup.
        /// </summary>
        public class TilePaletteActiveTargetsPopupUxmlTraits : UxmlTraits {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        private new static readonly string ussClassName = "unity-tilepalette-activetargets-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        private new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        private new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of input elements in elements of this type when warning is shown.
        /// </summary>
        private static readonly string inputWarningUssClassName = inputUssClassName + "--warning";
        /// <summary>
        /// USS class name of warning elements in elements of this type.
        /// </summary>
        private static readonly string warningUssClassName = ussClassName + "__warning";

        private readonly VisualElement m_WarningIconElement;

        private static List<GameObject> s_InvalidTargetsList = new List<GameObject>();

        /// <summary>
        /// Initializes and returns an instance of TilePaletteActiveTargetsPopup.
        /// </summary>
        public TilePaletteActiveTargetsPopup() : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of TilePaletteActiveTargetsPopup.
        /// </summary>
        /// <param name="label">Label name for the Popup</param>
        public TilePaletteActiveTargetsPopup(string label)
            : base(label
                , GridPaintingState.validTargets != null ? GridPaintingState.validTargets.ToList() : s_InvalidTargetsList
                , GetActiveTargetIndex())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            TilePaletteOverlayUtility.SetStyleSheet(this);

            labelElement.tooltip = k_LabelTooltip;

            m_WarningIconElement = new VisualElement();
            m_WarningIconElement.name = "Warning Icon";
            m_WarningIconElement.AddToClassList(warningUssClassName);
            m_WarningIconElement.tooltip = k_WarningTooltip;

            contentContainer.Add(m_WarningIconElement);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_FormatSelectedValueCallback += FormatSelectedValueCallback;
            createMenuCallback += CreateMenuCallback;

            UpdateTargets();
            SetValueWithoutNotify(GridPaintingState.scenePaintTarget);
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            GridPaintingState.scenePaintTargetChanged += OnScenePaintTargetChanged;
            GridPaintingState.validTargetsChanged += UpdateTargets;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            GridPaintingState.scenePaintTargetChanged -= OnScenePaintTargetChanged;
            GridPaintingState.validTargetsChanged -= UpdateTargets;
        }

        private string FormatSelectedValueCallback(GameObject go)
        {
            if (go != null)
                return go.name;
            return k_NullGameObjectName;
        }

        private IGenericMenu CreateMenuCallback()
        {
            return new TilePaletteActiveTargetsDropdownMenu();
        }

        private static int GetActiveTargetIndex()
        {
            if (GridPaintingState.validTargets == null)
                return -1;

            return Array.IndexOf(GridPaintingState.validTargets, GridPaintingState.scenePaintTarget);
        }

        private void OnScenePaintTargetChanged(GameObject _)
        {
            UpdateActiveTarget();
        }

        private void UpdateChoices()
        {
            choices.Clear();
            if (GridPaintingState.validTargets == null)
                return;

            foreach (var target in GridPaintingState.validTargets)
            {
                choices.Add(target);
            }
        }

        private void UpdateActiveTarget()
        {
            var newIndex = GetActiveTargetIndex();
            if (newIndex != -1 && choices.Count < newIndex)
            {
                UpdateChoices();
                newIndex = GetActiveTargetIndex();
            }
            index = newIndex;

            bool needWarning = TilePalettePrefabUtility.IsObjectPrefabInstance(GridPaintingState.scenePaintTarget);
            if (needWarning)
            {
                visualInput.AddToClassList(inputWarningUssClassName);
            }
            else
            {
                visualInput.RemoveFromClassList(inputWarningUssClassName);
            }
            m_WarningIconElement.visible = needWarning;
            m_WarningIconElement.style.position = needWarning ? Position.Relative : Position.Absolute;
        }

        private void UpdateTargets()
        {
            UpdateChoices();
            UpdateActiveTarget();
        }
    }

    /// <summary>
    /// Visual Element displaying the Icon for Active Target for Grid Painting.
    /// </summary>
    internal class TilePaletteActiveTargetsPopupIcon : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        private static readonly string ussClassName = "unity-tilepalette-activetargets-icon";

        private readonly string kTooltip = L10n.Tr("Active Target");

        private readonly string kIconPath = "Packages/com.unity.2d.tilemap/Editor/Icons/Tilemap.ActiveTargetLayers.png";

        /// <summary>
        /// Constructor for TilePaletteActiveTargetsPopupIcon
        /// </summary>
        public TilePaletteActiveTargetsPopupIcon()
        {
            AddToClassList(ussClassName);
            TilePaletteOverlayUtility.SetStyleSheet(this);
            tooltip = kTooltip;

            style.backgroundImage = EditorGUIUtility.LoadIcon(kIconPath);
        }
    }
}
