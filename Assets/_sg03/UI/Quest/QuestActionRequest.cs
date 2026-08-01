using System;
using System.Collections;
using SaiGame.Services;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>Quest action requests that treat a JSON <c>error</c> field as a failure, even for HTTP 2xx responses.</summary>
    internal static class QuestActionRequest
    {
        [Serializable]
        private class ErrorResponse
        {
            public string error;
        }

        public static void RunDefinitionAction(string questDefinitionId, string action, Action onSuccess, Action<string> onError)
        {
            SaiServer server = SaiServer.Instance;
            if (server == null)
            {
                onError?.Invoke("SaiServer not found!");
                return;
            }
            if (!server.IsAuthenticated)
            {
                onError?.Invoke("Not authenticated! Please login first.");
                return;
            }
            if (string.IsNullOrEmpty(questDefinitionId))
            {
                onError?.Invoke("questDefinitionId cannot be empty.");
                return;
            }

            string endpoint = $"/api/v1/games/{server.GameId}/quests/{questDefinitionId}/{action}";
            server.StartCoroutine(PostAction(server, endpoint, onSuccess, onError));
        }

        public static void RunDailyAssignmentAction(string assignmentId, string action, Action onSuccess, Action<string> onError)
        {
            SaiServer server = SaiServer.Instance;
            if (server == null)
            {
                onError?.Invoke("SaiServer not found!");
                return;
            }
            if (!server.IsAuthenticated)
            {
                onError?.Invoke("Not authenticated! Please login first.");
                return;
            }
            if (string.IsNullOrEmpty(assignmentId))
            {
                onError?.Invoke("assignmentId cannot be empty.");
                return;
            }

            string endpoint = $"/api/v1/games/{server.GameId}/daily-quest-assignments/{assignmentId}/{action}";
            server.StartCoroutine(PostAction(server, endpoint, onSuccess, onError));
        }

        private static IEnumerator PostAction(SaiServer server, string endpoint, Action onSuccess, Action<string> onError)
        {
            yield return server.PostRequest(endpoint, "{}",
                response =>
                {
                    if (HasError(response)) onError?.Invoke(response);
                    else onSuccess?.Invoke();
                },
                onError);
        }

        private static bool HasError(string response)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(JsonUtility.FromJson<ErrorResponse>(response)?.error);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
