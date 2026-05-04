using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(CardMovement))]
    public class CardMovementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CardMovement movement = (CardMovement)target;

            // ── Clear Face Up / Down ──────────────────────────────────────────────
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Clear Face Up Down", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Face Up"))
                movement.FaceUp();
            if (GUILayout.Button("Face Down"))
                movement.FaceDown();
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            // ── Unknown Face ──────────────────────────────────────────────────────
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Unknown Face", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Face Up Unknown"))
                movement.FaceUpUnknown();
            if (GUILayout.Button("Face Down Unknown"))
                movement.FaceDownUnknown();
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }
    }
}
