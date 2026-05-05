using SaiGame.Services;
using SG03.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    [AddComponentMenu("SG03/LampOfSoul/Lamp Click Detector")]
    public class LampClickDetector : SaiBehaviour
    {
        // ─── Linked Components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private BattleStateCtrl battleStateCtrl;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private BattleScripts battleScripts;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleStateCtrl();
            this.LoadMainCamera();
            this.LoadBattleScripts();
        }

        protected virtual void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = Object.FindFirstObjectByType<BattleStateCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        protected virtual void LoadMainCamera()
        {
            if (this.mainCamera != null) return;
            this.mainCamera = Camera.main;
            Debug.LogWarning(this.transform.name + ": LoadMainCamera", this.gameObject);
        }

        protected virtual void LoadBattleScripts()
        {
            if (this.battleScripts != null) return;
            this.battleScripts = Object.FindFirstObjectByType<BattleScripts>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadBattleScripts", this.gameObject);
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Update()
        {
            this.DetectClick();
        }

        // ─── Detection ───────────────────────────────────────────────────────────

        private void DetectClick()
        {
            if (!this.IsMouseButtonPressed()) return;
            if (!this.IsLampHit()) return;
            this.OnLampClicked();
        }

        private bool IsMouseButtonPressed()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private bool IsLampHit()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = this.mainCamera.ScreenPointToRay(mousePos);
            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject == this.gameObject) return true;
            }
            return false;
        }

        // ─── Private helpers ─────────────────────────────────────────────────────

        private void OnLampClicked()
        {
            this.LogLampClicked();
            this.DispatchByNextMove();
        }

        private void DispatchByNextMove()
        {
            if (this.battleStateCtrl?.BattleState == null) return;
            NextMoveType nextMove = this.battleStateCtrl.BattleState.NextMove;
            if (nextMove == NextMoveType.card_deploy) this.HandleCardDeploy();
        }

        private void HandleCardDeploy()
        {
            if (this.battleScripts == null) return;
            this.battleScripts.RunCardDeploy(this.OnCardDeploySuccess, this.OnCardDeployError);
        }

        private void OnCardDeploySuccess(string response)
        {
            Debug.Log("<color=#FF88FF><b>[LampClickDetector] Card deploy success</b></color> " + response);
            this.battleStateCtrl?.BattleState?.UpdateFromBattleStatus(response);
        }

        private void OnCardDeployError(string error)
        {
            Debug.LogError("[LampClickDetector] Card deploy error: " + error);
        }

        private void LogLampClicked()
        {
            int turn   = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.Turn   : 0;
            int action = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.Action : 0;
            string nextMove = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.NextMove.ToString() : "";
            Debug.Log($"<color=#FFD700><b>[LampClickDetector] Lamp clicked — Turn={turn}, Action={action}, nextMove={nextMove}</b></color>");
        }
    }
}
