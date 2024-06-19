using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    internal class TilePaletteActiveTargetsDropdownMenu : IGenericMenu
    {
        private const float k_ActiveTargetDropdownWidth = 190f;

        private GridPaintTargetsDropdown m_Dropdown;

        public TilePaletteActiveTargetsDropdownMenu()
        {
            int index = GridPaintingState.scenePaintTarget != null ? Array.IndexOf(GridPaintingState.validTargets, GridPaintingState.scenePaintTarget) : 0;
            var menuData = new GridPaintTargetsDropdown.MenuItemProvider();
            m_Dropdown = new GridPaintTargetsDropdown(menuData, index, null, SelectTarget, k_ActiveTargetDropdownWidth);
        }

        public void AddItem(string itemName, bool isChecked, System.Action action)
        {
        }

        public void AddItem(string itemName, bool isChecked, System.Action<object> action, object data)
        {
        }

        public void AddDisabledItem(string itemName, bool isChecked)
        {
        }

        public void AddSeparator(string path)
        {
        }

        public void DropDown(Rect position, VisualElement targetElement = null, bool anchored = false)
        {
            PopupWindow.Show(position, m_Dropdown);
        }

        private static void SelectTarget(int i, object o)
        {
            var obj = o as GameObject;
            var isPrefabInstance = TilePalettePrefabUtility.IsObjectPrefabInstance(obj);
            if (isPrefabInstance)
            {
                var editMode = (TilePaletteActiveTargetsProperties.PrefabEditModeSettings)EditorPrefs.GetInt(TilePaletteActiveTargetsProperties.targetEditModeEditorPref, 0);
                switch (editMode)
                {
                    case TilePaletteActiveTargetsProperties.PrefabEditModeSettings.EnableDialog:
                    {
                        var option = EditorUtility.DisplayDialogComplex(TilePaletteActiveTargetsProperties.targetEditModeDialogTitle
                            , TilePaletteActiveTargetsProperties.targetEditModeDialogMessage
                            , TilePaletteActiveTargetsProperties.targetEditModeDialogYes
                            , TilePaletteActiveTargetsProperties.targetEditModeDialogNo
                            , TilePaletteActiveTargetsProperties.targetEditModeDialogChange);
                        switch (option)
                        {
                            case 0:
                                TilePalettePrefabUtility.GoToPrefabMode(obj);
                                return;
                            case 1:
                                // Do nothing here for "No"
                                break;
                            case 2:
                                var settingsWindow = SettingsWindow.Show(SettingsScope.User);
                                settingsWindow.FilterProviders(TilePaletteActiveTargetsProperties.targetEditModeLookup);
                                break;
                        }
                    }
                    break;
                    case TilePaletteActiveTargetsProperties.PrefabEditModeSettings.EditInPrefabMode:
                        TilePalettePrefabUtility.GoToPrefabMode(obj);
                        return;
                }
            }

            GridPaintingState.scenePaintTarget = obj;
        }
    }
}
