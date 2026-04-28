using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(CardReviewMovement))]
    public class CardReviewMovementEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CardReviewMovement movement = (CardReviewMovement)target;

            // ── Fly Up ────────────────────────────────────────────────────────────
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Show", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Show"))
                movement.Show();
            GUI.enabled = true;

            // ── Hide ──────────────────────────────────────────────────────────────
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Hide", EditorStyles.boldLabel);

            SerializedProperty originProp = serializedObject.FindProperty("originPosition");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(originProp, new GUIContent("Origin Position (Start)"));
            EditorGUI.EndDisabledGroup();

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Hide"))
                movement.Hide();
            GUI.enabled = true;
        }
    }
}
