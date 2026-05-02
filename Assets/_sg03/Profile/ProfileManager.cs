using UnityEngine;
using SaiGame.Services;

namespace SG03
{
    // Handles gamer progress lifecycle on login:
    //   1. Fetches progress from the server.
    //   2. If no progress exists, creates a new one automatically.
    //   3. Stores the loaded progress in serialized fields for Inspector display.
    public class ProfileManager : SaiBehaviour
    {
        [Header("Gamer Progress")]
        [SerializeField] private string progressId;
        [SerializeField] private string userId;
        [SerializeField] private int    level;
        [SerializeField] private int    experience;
        [SerializeField] private int    gold;
        [SerializeField][TextArea(3, 6)] private string gameData;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.RegisterLoginListener();
        }

        private void RegisterLoginListener()
        {
            if (SaiServer.Instance?.SaiAuth == null) return;
            SaiServer.Instance.SaiAuth.OnLoginSuccess += this.OnLoginSuccess;
        }

        protected virtual void OnDestroy()
        {
            if (SaiServer.Instance?.SaiAuth != null)
                SaiServer.Instance.SaiAuth.OnLoginSuccess -= this.OnLoginSuccess;
        }

        // Called automatically when the user logs in successfully.
        private void OnLoginSuccess(LoginResponse _) => this.LoadOrCreateProgress();

        // Step 1: try to fetch existing progress.
        private void LoadOrCreateProgress()
        {
            if (SaiServer.Instance?.GamerProgress == null)
            {
                Debug.LogWarning("[ProfileManager] GamerProgress service not found on SaiServer.");
                return;
            }

            SaiServer.Instance.GamerProgress.GetProgress(
                onSuccess: this.OnGetProgressSuccess,
                onError:   this.OnGetProgressError
            );
        }

        // Step 2a: progress found — display it in the Inspector.
        private void OnGetProgressSuccess(GamerProgressData data)
        {
            this.ApplyToInspector(data);
            // Debug.Log($"[ProfileManager] Progress loaded — Level {data.level}, XP {data.experience}, Gold {data.gold}");
        }

        // Step 2b: no progress yet — create a new one.
        private void OnGetProgressError(string error)
        {
            Debug.Log($"[ProfileManager] No progress found ({error}), creating new progress...");
            this.CreateProgress();
        }

        // Step 3: call the create API.
        private void CreateProgress()
        {
            if (SaiServer.Instance?.GamerProgress == null) return;

            SaiServer.Instance.GamerProgress.CreateProgress(
                onSuccess: this.OnCreateProgressSuccess,
                onError:   this.OnCreateProgressError
            );
        }

        private void OnCreateProgressSuccess(GamerProgressData data)
        {
            this.ApplyToInspector(data);
            Debug.Log($"[ProfileManager] Progress created — ID {data.id}");
        }

        private void OnCreateProgressError(string error)
        {
            Debug.LogWarning($"[ProfileManager] Failed to create progress: {error}");
        }

        // Writes GamerProgressData fields into serialized fields for Inspector display.
        private void ApplyToInspector(GamerProgressData data)
        {
            if (data == null) return;
            this.progressId  = data.id;
            this.userId      = data.user_id;
            this.level       = data.level;
            this.experience  = data.experience;
            this.gold        = data.gold;
            this.gameData    = data.game_data;
        }
    }
}
