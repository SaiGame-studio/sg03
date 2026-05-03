using SaiGame.Services;
using UnityEditor;
using UnityEngine;

namespace SG03.Editor
{
    [CustomEditor(typeof(BattleCardDefinitions))]
    public class BattleCardDefinitionsEditor : UnityEditor.Editor
    {
        private BattleCardDefinitions battleCardDefinitions;
        private SerializedProperty jsonResponse;
        private SerializedProperty codes;
        private SerializedProperty definitions;
        private Vector2 jsonScroll;
        private bool showJson = true;
        private bool showDefinitions = true;

        private void OnEnable()
        {
            this.battleCardDefinitions = (BattleCardDefinitions)this.target;
            this.jsonResponse = this.serializedObject.FindProperty("jsonResponse");
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

            EditorGUILayout.Space(10);
            this.DrawJsonResponse();

            EditorGUILayout.Space(6);
            this.DrawDefinitionsCache();

            this.serializedObject.ApplyModifiedProperties();
        }

        private void DrawJsonResponse()
        {
            this.showJson = EditorGUILayout.Foldout(this.showJson, "Raw JSON Response", true);
            if (!this.showJson) return;

            if (string.IsNullOrEmpty(this.jsonResponse.stringValue))
            {
                EditorGUILayout.HelpBox("No data yet. Click \"Get All\" to fetch card definitions.", MessageType.None);
                return;
            }

            GUIStyle jsonStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = false,
                fontSize = 11,
                richText = false
            };

            GUIContent content = new GUIContent(this.jsonResponse.stringValue);
            Vector2 contentSize = jsonStyle.CalcSize(content);
            float scrollHeight = Mathf.Min(contentSize.y + 6f, 300f);

            this.jsonScroll = EditorGUILayout.BeginScrollView(
                this.jsonScroll,
                alwaysShowHorizontal: true,
                alwaysShowVertical: false,
                GUILayout.Height(scrollHeight));

            EditorGUILayout.SelectableLabel(
                this.jsonResponse.stringValue,
                jsonStyle,
                GUILayout.Height(contentSize.y),
                GUILayout.Width(contentSize.x));

            EditorGUILayout.EndScrollView();
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
