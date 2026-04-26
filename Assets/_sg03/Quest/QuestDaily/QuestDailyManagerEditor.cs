#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using SaiGame.Services;

namespace SG03.Quest
{
    [CustomEditor(typeof(QuestDailyManager))]
    public class QuestDailyManagerEditor : Editor
    {
        private QuestDailyManager manager;
        private bool isLoadingPools = false;

        private void OnEnable()
        {
            this.manager = target as QuestDailyManager;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw all default fields first
            DrawDefaultInspector();

            EditorGUILayout.Space(6);

            // ── Get Pools button ──────────────────────────────────────────
            GUI.backgroundColor = this.isLoadingPools ? Color.gray : new Color(0.4f, 0.8f, 1f);
            EditorGUI.BeginDisabledGroup(this.isLoadingPools || !Application.isPlaying);
            if (GUILayout.Button(this.isLoadingPools ? "Loading..." : "Get Pools", GUILayout.Height(30)))
                this.RunGetPools();
            EditorGUI.EndDisabledGroup();
            GUI.backgroundColor = Color.white;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use Get Pools.", MessageType.None);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RunGetPools()
        {
            if (SaiServer.Instance == null)
            {
                Debug.LogError("[QuestDailyManagerEditor] SaiServer not found!");
                return;
            }

            if (!SaiServer.Instance.IsAuthenticated)
            {
                Debug.LogError("[QuestDailyManagerEditor] Not authenticated! Please login first.");
                return;
            }

            this.isLoadingPools = true;
            Repaint();

            SaiServer.Instance.DailyQuest.GetPools(
                onSuccess: response =>
                {
                    this.isLoadingPools = false;
                    Debug.Log($"[QuestDailyManagerEditor] Loaded {response.pools?.Length ?? 0} pools");
                    Repaint();
                },
                onError: error =>
                {
                    this.isLoadingPools = false;
                    Debug.LogError($"[QuestDailyManagerEditor] Failed to load pools: {error}");
                    Repaint();
                }
            );
        }
    }
}
#endif
