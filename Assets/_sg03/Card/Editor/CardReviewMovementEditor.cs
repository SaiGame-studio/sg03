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
            EditorGUILayout.LabelField("Fly Up", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Fly Up"))
                movement.FlyUp();
            GUI.enabled = true;

            // ── Fly Down ──────────────────────────────────────────────────────────
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Fly Down", EditorStyles.boldLabel);

            SerializedProperty originProp = serializedObject.FindProperty("originPosition");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(originProp, new GUIContent("Origin Position (Start)"));
            EditorGUI.EndDisabledGroup();

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Fly Down"))
                movement.FlyDown();
            GUI.enabled = true;
        }
    }
}
