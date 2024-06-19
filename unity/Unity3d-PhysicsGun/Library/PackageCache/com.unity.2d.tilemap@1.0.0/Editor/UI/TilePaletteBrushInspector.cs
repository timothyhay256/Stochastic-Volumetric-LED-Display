using UnityEngine;

namespace UnityEditor.Tilemaps
{
    internal class TilePaletteBrushInspector
    {
        private Vector2 m_Scroll;

        private static class Styles
        {
            public static readonly GUIContent lockZPosition = EditorGUIUtility.TrTextContent("Lock Z Position", "Toggle editing of Z position");
            public static readonly GUIContent zPosition = EditorGUIUtility.TrTextContent("Z Position", "Set a Z position for the active Brush for painting");
            public static readonly GUIContent resetZPosition = EditorGUIUtility.TrTextContent("Reset", "Reset Z position for the active Brush");
        }

        public void OnGUI()
        {
            if (GridPaintingState.gridBrush == null)
                return;

            m_Scroll = GUILayout.BeginScrollView(m_Scroll);

            // Brush Inspector GUI
            EditorGUI.BeginChangeCheck();
            if (GridPaintingState.activeBrushEditor != null)
                GridPaintingState.activeBrushEditor.OnPaintInspectorGUI();
            else if (GridPaintingState.fallbackEditor != null)
                GridPaintingState.fallbackEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                GridPaletteBrushes.ActiveGridBrushAssetChanged();
            }

            // Z Position Inspector
            var hasLastActiveGrid = GridPaintingState.lastActiveGrid != null;
            using (new EditorGUI.DisabledScope(!hasLastActiveGrid))
            {
                var lockZPosition = false;
                if (GridPaintingState.activeBrushEditor != null)
                {
                    EditorGUI.BeginChangeCheck();
                    lockZPosition = EditorGUILayout.Toggle(Styles.lockZPosition, !GridPaintingState.activeBrushEditor.canChangeZPosition);
                    if (EditorGUI.EndChangeCheck())
                        GridPaintingState.activeBrushEditor.canChangeZPosition = !lockZPosition;
                }
                using (new EditorGUI.DisabledScope(lockZPosition))
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    var zPosition = EditorGUILayout.DelayedIntField(Styles.zPosition, hasLastActiveGrid ? GridPaintingState.lastActiveGrid.zPosition : 0);
                    if (EditorGUI.EndChangeCheck())
                    {
                        GridPaintingState.gridBrush.ChangeZPosition(zPosition - GridPaintingState.lastActiveGrid.zPosition);
                        GridPaintingState.lastActiveGrid.zPosition = zPosition;
                    }
                    if (GUILayout.Button(Styles.resetZPosition))
                    {
                        GridPaintingState.gridBrush.ResetZPosition();
                        GridPaintingState.lastActiveGrid.ResetZPosition();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }
    }
}
