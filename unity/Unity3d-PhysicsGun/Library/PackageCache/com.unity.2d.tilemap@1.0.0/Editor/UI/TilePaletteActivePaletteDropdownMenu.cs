using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    internal class TilePaletteActivePaletteDropdownMenu : IGenericMenu
    {
        private const float k_DropdownWidth = 150f;

        private GridPalettesDropdown m_Dropdown;

        public TilePaletteActivePaletteDropdownMenu()
        {
            int index = GridPaintingState.palettes != null ? GridPaintingState.palettes.IndexOf(GridPaintingState.palette) : 0;
            var menuData = new GridPalettesDropdown.MenuItemProvider();
            m_Dropdown = new GridPalettesDropdown(menuData, index, null, SelectPalette, k_DropdownWidth);
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

        private void SelectPalette(int i, object o)
        {
            if (i < GridPaintingState.palettes.Count)
            {
                GridPaintingState.palette = GridPaintingState.palettes[i];
            }
            else
            {
                m_Dropdown.editorWindow.Close();
                OpenAddPalettePopup(new Rect(0, 0, 0, 0));
            }
        }

        private void OpenAddPalettePopup(Rect rect)
        {
            bool popupOpened = GridPaletteAddPopup.ShowAtPosition(rect);
            if (popupOpened)
                GUIUtility.ExitGUI();
        }
    }
}
