using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(CardLoader))]
    public class CardLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Load Controls", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Load Card"))
                _ = ((CardLoader)target).LoadAndApply();

            GUI.enabled = true;
        }
    }
}
