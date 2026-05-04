using SaiGame.Services;
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

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadBattleStateCtrl();
            this.LoadMainCamera();
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
            this.LogLampClicked();
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

        private void LogLampClicked()
        {
            int turn   = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.Turn   : 0;
            int action = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.Action : 0;
            string nextMove = this.battleStateCtrl?.BattleState != null ? this.battleStateCtrl.BattleState.NextMove.ToString() : "";
            Debug.Log($"<color=#FFD700><b>[LampClickDetector] Lamp clicked — Turn={turn}, Action={action}, nextMove={nextMove}</b></color>");
        }
    }
}
