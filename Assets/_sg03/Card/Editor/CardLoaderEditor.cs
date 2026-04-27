using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(CardLoader))]
    public class CardLoaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Load Controls", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Load Card by Address"))
                _ = ((CardLoader)target).LoadAndApply();

            if (GUILayout.Button("Load Card By Name"))
                LoadByName((CardLoader)target);

            GUI.enabled = true;
        }

        private void LoadByName(CardLoader loader)
        {
            if (CardDataManager.Instance == null)
            {
                Debug.LogWarning("[CardLoaderEditor] CardDataManager instance not found in scene.", loader);
                return;
            }

            string prefix = serializedObject.FindProperty("cardNamePrefix").stringValue;

            string found = CardDataManager.Instance.CardAddresses
                .FirstOrDefault(a => a.Contains(prefix));

            if (string.IsNullOrEmpty(found))
            {
                Debug.LogWarning($"[CardLoaderEditor] No address found containing '{prefix}'.", loader);
                return;
            }

            serializedObject.FindProperty("cardAddress").stringValue = found;
            serializedObject.ApplyModifiedProperties();

            _ = loader.LoadAndApply();
        }
    }
}
