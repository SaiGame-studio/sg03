using UnityEditor;
using UnityEngine;

namespace SG03.UI
{
    [CustomEditor(typeof(VoidManagerGrid))]
    public class VoidManagerGridEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            DrawDefaultInspector();
            this.serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledGroupScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Refresh UI", GUILayout.Height(30f)))
                {
                    VoidManagerGrid grid = (VoidManagerGrid)this.target;
                    grid.RefreshUI();
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to refresh the runtime UI.", MessageType.None);
            }
        }
    }
}
