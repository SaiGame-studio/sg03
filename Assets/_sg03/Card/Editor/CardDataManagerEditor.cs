using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(CardDataManager))]
    public class CardDataManagerEditor : Editor
    {
        private string testAddress = "Cards/CardData_Example";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Runtime Test", EditorStyles.boldLabel);

            testAddress = EditorGUILayout.TextField("Card Address", testAddress);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Show Card"))
                _ = ((CardDataManager)target).ShowCardAsync(testAddress);

            if (GUILayout.Button("Hide Card"))
                ((CardDataManager)target).HideCard();
            GUI.enabled = true;
        }
    }
}
