using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    internal static class TilePaletteOverlayUtility
    {
        private static readonly string s_TilePaletteOverlayStyleSheetPath = "Packages/com.unity.2d.tilemap/Editor/UI/TilePaletteOverlay.uss";
        private static readonly string s_TilePaletteOverlayStyleSheetShiroPath = "Packages/com.unity.2d.tilemap/Editor/UI/TilePaletteOverlayLight.uss";
        private static readonly string s_TilePaletteOverlayStyleSheetKuroPath = "Packages/com.unity.2d.tilemap/Editor/UI/TilePaletteOverlayDark.uss";

        private static StyleSheet s_TilePaletteOverlayStyleSheet;
        private static StyleSheet s_TilePaletteOverlayStyleSheetLight;
        private static StyleSheet s_TilePaletteOverlayStyleSheetDark;
        private static StyleSheet StyleSheet
        {
            get
            {
                if (s_TilePaletteOverlayStyleSheet == null)
                    s_TilePaletteOverlayStyleSheet = EditorGUIUtility.Load(s_TilePaletteOverlayStyleSheetPath) as StyleSheet;
                return s_TilePaletteOverlayStyleSheet;
            }
        }

        private static StyleSheet StyleSheetLight
        {
            get
            {
                if (s_TilePaletteOverlayStyleSheetLight == null)
                    s_TilePaletteOverlayStyleSheetLight = EditorGUIUtility.Load(s_TilePaletteOverlayStyleSheetShiroPath) as StyleSheet;
                return s_TilePaletteOverlayStyleSheetLight;
            }
        }

        private static StyleSheet StyleSheetDark
        {
            get
            {
                if (s_TilePaletteOverlayStyleSheetDark == null)
                    s_TilePaletteOverlayStyleSheetDark = EditorGUIUtility.Load(s_TilePaletteOverlayStyleSheetKuroPath) as StyleSheet;
                return s_TilePaletteOverlayStyleSheetDark;
            }
        }

        internal static void SetStyleSheet(VisualElement ve)
        {
            ve.styleSheets.Add(StyleSheet);
            if (EditorGUIUtility.isProSkin)
                ve.styleSheets.Add(StyleSheetDark);
            else
                ve.styleSheets.Add(StyleSheetLight);
        }
    }
}
