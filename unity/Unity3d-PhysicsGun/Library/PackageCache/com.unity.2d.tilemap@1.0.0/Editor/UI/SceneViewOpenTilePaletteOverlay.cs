using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    [Overlay(typeof(SceneView), k_OverlayId, k_DisplayName
        , defaultDockZone = DockZone.RightColumn
        , defaultDockPosition = DockPosition.Bottom
        , defaultDockIndex = 0
        , defaultLayout = Layout.Panel)]
    internal class SceneViewOpenTilePaletteOverlay : ToolbarOverlay, ITransientOverlay
    {
        internal const string k_OverlayId = "Scene View/Open Tile Palette";
        private const string k_DisplayName = "Open Tile Palette";

        public SceneViewOpenTilePaletteOverlay() : base(TilePaletteOpenPalette.k_ToolbarId) {}

        public bool visible => SceneViewOpenTilePaletteHelper.showInSceneViewActive && SceneViewOpenTilePaletteHelper.IsActive();
    }

    [EditorToolbarElement(k_ToolbarId)]
    sealed class TilePaletteOpenPalette : EditorToolbarButton
    {
        internal const string k_ToolbarId = "Tile Palette/Open Palette";

        const string k_ToolSettingsClass = "unity-tool-settings";

        private static string k_LabelText = L10n.Tr("Open Tile Palette");
        private static string k_TooltipText = L10n.Tr("Opens the Tile Palette Window");
        private const string k_IconPath = "Packages/com.unity.2d.tilemap/Editor/Icons/Tilemap.TilePalette.png";

        public TilePaletteOpenPalette() : base(SceneViewOpenTilePaletteHelper.OpenTilePalette)
        {
            name = "Open Tile Palette";
            AddToClassList(k_ToolSettingsClass);

            icon = EditorGUIUtility.LoadIconRequired(k_IconPath);
            text = k_LabelText;
            tooltip = k_TooltipText;
        }
    }
}
