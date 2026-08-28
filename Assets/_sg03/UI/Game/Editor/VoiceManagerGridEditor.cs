using UnityEditor;
using UnityEngine;

namespace SG03.UI
{
    [CustomEditor(typeof(VoiceManagerGrid))]
    public class VoiceManagerGridEditor : UnityEditor.Editor
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
                    VoiceManagerGrid grid = (VoiceManagerGrid)this.target;
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
