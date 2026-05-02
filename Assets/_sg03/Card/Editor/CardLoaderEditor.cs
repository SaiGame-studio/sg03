using UnityEditor;
using UnityEngine;

namespace SG03
{
    [CustomEditor(typeof(CardLoader))]
    public class CardLoaderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            this.DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Load Controls", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Load Card by Address"))
                _ = ((CardLoader)this.target).LoadAndApply();

            if (GUILayout.Button("Load Card By Name"))
                this.LoadByName((CardLoader)this.target);

            GUI.enabled = true;
        }

        private void LoadByName(CardLoader loader)
        {
            if (CardDataManager.Instance == null)
            {
                Debug.LogWarning("[CardLoaderEditor] CardDataManager instance not found in scene.", loader);
                return;
            }

            string prefix = this.serializedObject.FindProperty("cardNamePrefix").stringValue;

            if (!CardLoader.TryResolveAddressByAssetName(CardDataManager.Instance.CardAddresses, prefix, out string found))
            {
                Debug.LogWarning($"[CardLoaderEditor] No address found ending with '{prefix}.asset'.", loader);
                return;
            }

            this.serializedObject.FindProperty("cardAddress").stringValue = found;
            this.serializedObject.ApplyModifiedProperties();

            _ = loader.LoadAndApply();
        }
    }
}
