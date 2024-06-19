using UnityEngine.UIElements;

namespace UnityEditor.Tilemaps
{
    /// <summary>
    /// Visual Element showing the Inspector for the Active Brush for Grid Painting.
    /// </summary>
    public class TilePaletteBrushInspectorElement : IMGUIContainer
    {
        /// <summary>
        /// Factory for TilePaletteBrushInspectorElement.
        /// </summary>
        public class TilePaletteBrushInspectorElementFactory : UxmlFactory<TilePaletteBrushInspectorElement, TilePaletteBrushInspectorElementUxmlTraits> {}
        /// <summary>
        /// UxmlTraits for TilePaletteBrushInspectorElement.
        /// </summary>
        public class TilePaletteBrushInspectorElementUxmlTraits : UxmlTraits {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        private new static readonly string ussClassName = "unity-tilepalette-brushinspector";

        private TilePaletteBrushInspector m_BrushInspector = new TilePaletteBrushInspector();

        /// <summary>
        /// Initializes and returns an instance of TilePaletteBrushInspectorElement.
        /// </summary>
        public TilePaletteBrushInspectorElement()
        {
            AddToClassList(ussClassName);
            TilePaletteOverlayUtility.SetStyleSheet(this);

            onGUIHandler = m_BrushInspector.OnGUI;
        }
    }
}
