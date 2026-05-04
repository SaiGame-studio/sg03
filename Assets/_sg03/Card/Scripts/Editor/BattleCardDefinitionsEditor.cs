using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    [CustomEditor(typeof(BattleCardDefinitions))]
    public class BattleCardDefinitionsEditor : UnityEditor.Editor
    {
        private BattleCardDefinitions battleCardDefinitions;
        private SerializedProperty codes;
        private SerializedProperty definitions;
        private bool showDefinitions = true;

        private void OnEnable()
        {
            this.battleCardDefinitions = (BattleCardDefinitions)this.target;
            this.codes = this.serializedObject.FindProperty("codes");
            this.definitions = this.serializedObject.FindProperty("definitions");
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            EditorGUILayout.LabelField("Battle Card Definitions", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            GUI.backgroundColor = new Color(0.3f, 0.9f, 0.5f);
            if (GUILayout.Button("Get All", GUILayout.Height(30)))
            {
                this.battleCardDefinitions.GetAll();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(6);
            this.DrawDefinitionsCache();

            this.serializedObject.ApplyModifiedProperties();
        }

        private void DrawDefinitionsCache()
        {
            int count = this.definitions.arraySize;
            string header = $"Definitions Cache ({count} cards)";
            this.showDefinitions = EditorGUILayout.Foldout(this.showDefinitions, header, true);
            if (!this.showDefinitions) return;

            if (count == 0)
            {
                EditorGUILayout.HelpBox("Cache is empty. Click \"Get All\" to load definitions.", MessageType.None);
                return;
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(this.codes, new GUIContent("Codes"), true);
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(this.definitions, new GUIContent("Definitions"), true);
            EditorGUI.indentLevel--;
        }
    }
}
