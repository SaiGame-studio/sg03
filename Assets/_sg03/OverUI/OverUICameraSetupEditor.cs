#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(OverUICameraSetup))]
    public class OverUICameraSetupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);

            OverUICameraSetup setup = (OverUICameraSetup)target;

            // ── Configure + Apply button ──────────────────────────────────────
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.6f);
            if (GUILayout.Button("Configure Camera & Apply", GUILayout.Height(32)))
            {
                Undo.RecordObject(setup, "Configure OverUI Camera");
                setup.ConfigureCamera();
                EditorUtility.SetDirty(setup);
            }

            GUI.backgroundColor = Color.white;

            // ── Info box ──────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Why RenderTexture instead of camera stack?\n" +
                "UI Toolkit draws AFTER all URP cameras, so overlay cameras always\n" +
                "appear behind UI Toolkit. The fix: render the 3D object into a\n" +
                "RenderTexture, then show it in a separate UIDocument with a\n" +
                "higher sort order — guaranteed to draw on top.\n\n" +
                "One-time setup:\n" +
                "1. Add layer \"OverUI\"  (Project Settings > Tags and Layers).\n" +
                "2. Create a Camera — Culling Mask: OverUI only.\n" +
                "   (Clear Flags / background alpha are set by Configure button)\n" +
                "3. Create an empty GameObject with UIDocument component.\n" +
                "   (PanelSettings is created at runtime — no need to assign one)\n" +
                "4. Assign Over UI Camera + Overlay Document in Inspector.\n" +
                "5. Click \"Configure Camera & Apply\".\n" +
                "6. Set any 3D object's Layer to \"OverUI\".",
                MessageType.Info);
        }
    }
}
#endif

