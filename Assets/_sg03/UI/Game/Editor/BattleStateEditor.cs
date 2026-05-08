using UnityEditor;
using UnityEngine;

namespace SG03.UI
{
    [CustomEditor(typeof(BattleState))]
    public class BattleStateEditor : UnityEditor.Editor
    {
        private bool debugLogFoldout = true;

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();
            DrawPropertiesExcluding(this.serializedObject, "debugLog");
            this.serializedObject.ApplyModifiedProperties();

            this.DrawDebugLog();

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Clear Data"))
            {
                BattleState battleState = (BattleState)target;
                Undo.RecordObject(battleState, "Clear BattleState Data");
                battleState.ClearData();
                EditorUtility.SetDirty(battleState);
            }
        }

        private void DrawDebugLog()
        {
            SerializedProperty prop = this.serializedObject.FindProperty("debugLog");
            if (prop == null) return;

            this.debugLogFoldout = EditorGUILayout.Foldout(this.debugLogFoldout, $"Debug Log ({prop.arraySize})", true);
            if (!this.debugLogFoldout) return;

            EditorGUI.indentLevel++;
            for (int i = 0; i < prop.arraySize; i++)
            {
                EditorGUILayout.SelectableLabel(
                    prop.GetArrayElementAtIndex(i).stringValue,
                    EditorStyles.miniLabel,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
            EditorGUI.indentLevel--;
        }
    }
}
