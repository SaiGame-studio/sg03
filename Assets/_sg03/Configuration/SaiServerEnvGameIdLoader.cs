using System.IO;
using SaiGame.Services;
using UnityEngine;

namespace SG03.Configuration
{
    /// <summary>Loads GAME_ID from the project-root .env file before scene Start methods run.</summary>
    public static class SaiServerEnvGameIdLoader
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadGameId()
        {
            SaiServer server = SaiServer.Instance;
            if (server == null) return;
            if (!string.IsNullOrWhiteSpace(server.GameId)) return;

            string envPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ".env");
            if (!File.Exists(envPath)) return;

            foreach (string line in File.ReadAllLines(envPath))
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#")) continue;

                int separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex <= 0) continue;

                string key = trimmedLine.Substring(0, separatorIndex).Trim();
                if (key != "GAME_ID") continue;

                string gameId = trimmedLine.Substring(separatorIndex + 1).Trim().Trim('"', '\'');
                if (string.IsNullOrWhiteSpace(gameId)) return;

                server.SetGameId(gameId);
                Debug.Log("[SaiServerEnvGameIdLoader] Loaded GAME_ID from .env.");
                return;
            }
        }
    }
}
