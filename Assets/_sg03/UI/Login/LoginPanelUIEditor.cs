using UnityEditor;
using UnityEngine;

namespace SG03.UI
{
    [CustomEditor(typeof(LoginPanelUI))]
    public class LoginPanelUIEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                Color prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);

                if (GUILayout.Button("Load Auth Credentials into UI"))
                {
                    ((LoginPanelUI)this.target).LoadCredentialsFromAuth();
                }

                GUI.backgroundColor = prev;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to load credentials.", MessageType.Info);
            }
        }
    }
}
