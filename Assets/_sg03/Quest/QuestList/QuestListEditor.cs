#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SG03.Quest
{
    [CustomEditor(typeof(QuestList))]
    public class QuestListEditor : Editor
    {
        private QuestList questList;
        private bool isRefreshing = false;

        private void OnEnable()
        {
            this.questList = (QuestList)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            // ── Refresh button ────────────────────────────────────────────
            bool canRefresh = Application.isPlaying && !this.isRefreshing;
            GUI.backgroundColor = this.isRefreshing ? Color.gray : new Color(0.4f, 0.8f, 1f);
            EditorGUI.BeginDisabledGroup(!canRefresh);
            if (GUILayout.Button(this.isRefreshing ? "Refreshing..." : "Refresh", GUILayout.Height(30)))
                this.RunRefresh();
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play Mode to use Refresh.", MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        private void RunRefresh()
        {
            if (this.questList.Manager == null)
            {
                Debug.LogError("[QuestListEditor] No QuestPoolManager found on parent.");
                return;
            }

            this.isRefreshing = true;
            Repaint();

            this.questList.Manager.RefreshList(
                this.questList.PoolKey,
                this.questList.RefreshMode,
                this.questList.DaysAhead,
                onSuccess: entries =>
                {
                    this.questList.SetEntries(entries);
                    this.isRefreshing = false;
                    Debug.Log($"[QuestListEditor] Refreshed '{this.questList.PoolKey}' — {entries?.Length ?? 0} entries");
                    Repaint();
                },
                onError: error =>
                {
                    this.isRefreshing = false;
                    Debug.LogError($"[QuestListEditor] Refresh failed: {error}");
                    Repaint();
                }
            );
        }
    }
}
#endif
