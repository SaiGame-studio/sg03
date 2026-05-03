using SaiGame.Services;
using SG03.UI;
using UnityEngine;

namespace SG03
{
    [AddComponentMenu("SG03/LampOfSoul/Lamp Of Soul Ctrl")]
    [RequireComponent(typeof(LampMovement))]
    public class LampOfSoulCtrl : SaiBehaviour
    {
        // ─── Linked Components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private LampMovement movement;
        [SerializeField] private DeskPositionCtrl deskPosition;
        [SerializeField] private BattleState battleState;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadLampMovement();
            this.LoadDeskPositionCtrl();
            this.LoadBattleState();
        }

        protected virtual void LoadLampMovement()
        {
            if (this.movement != null) return;
            this.movement = this.GetComponent<LampMovement>();
            Debug.LogWarning(this.transform.name + ": LoadLampMovement", this.gameObject);
        }

        protected virtual void LoadDeskPositionCtrl()
        {
            if (this.deskPosition != null) return;
            this.deskPosition = Object.FindFirstObjectByType<DeskPositionCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadDeskPositionCtrl", this.gameObject);
        }

        protected virtual void LoadBattleState()
        {
            if (this.battleState != null) return;
            this.battleState = Object.FindFirstObjectByType<BattleState>(FindObjectsInactive.Include);
            Debug.LogWarning(this.transform.name + ": LoadBattleState", this.gameObject);
        }

        private void OnEnable()
        {
            this.Subscribe();
        }

        private void OnDisable()
        {
            this.Unsubscribe();
        }

        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            BattleState.OnGameStart  += this.OnGameStart;
            BattleState.OnGameResume += this.OnGameResume;
        }

        private void Unsubscribe()
        {
            BattleState.OnGameStart  -= this.OnGameStart;
            BattleState.OnGameResume -= this.OnGameResume;
        }

        // ─── Event handlers ───────────────────────────────────────────────────────

        private void OnGameStart()
        {
            this.CallInitPosition();
        }

        private void OnGameResume()
        {
            this.CallInitPosition();
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private void CallInitPosition()
        {
            if (this.battleState == null) return;
            if (this.deskPosition == null) return;
            if (this.deskPosition.AlphaLampPosition == null) return;
            if (this.deskPosition.OmegaLampPosition == null) return;
            this.movement.InitPosition(
                this.battleState.Turn,
                this.deskPosition.AlphaLampPosition,
                this.deskPosition.OmegaLampPosition);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Moves the lamp to the specified target transform.</summary>
        public void MoveTo(Transform target) => this.movement.MoveTo(target);

        /// <summary>Returns the movement component of this lamp.</summary>
        public LampMovement Movement => this.movement;
    }
}
