using UnityEditor;
using UnityEngine;

namespace SG03.UI
{
    [CustomEditor(typeof(BattleState))]
    public class BattleStateEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Clear Data"))
            {
                BattleState battleState = (BattleState)target;
                Undo.RecordObject(battleState, "Clear BattleState Data");
                battleState.ClearData();
                EditorUtility.SetDirty(battleState);
            }
        }
    }
}
