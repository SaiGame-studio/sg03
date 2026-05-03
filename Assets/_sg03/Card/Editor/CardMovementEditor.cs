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

            // ── FaceUp ────────────────────────────────────────────────────────────
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Face Controls", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Face Up"))
                movement.FaceUp();

            if (GUILayout.Button("Face Down"))
                movement.FaceDown();
            GUI.enabled = true;
        }
    }
}
