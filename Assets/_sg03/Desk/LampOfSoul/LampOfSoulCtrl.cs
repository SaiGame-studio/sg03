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

        [Header("State")]
        [SerializeField] private bool isAtAlpha = false;
        public bool IsAtAlpha => this.isAtAlpha;

        private enum OptimisticDestination
        {
            None,
            Alpha,
            Omega
        }

        private OptimisticDestination optimisticDestination;

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
            BattleState.OnGameStart += this.OnGameStart;
        }

        private void Unsubscribe()
        {
            BattleState.OnGameStart -= this.OnGameStart;
        }

        // ─── Event handlers ───────────────────────────────────────────────────────

        private void OnGameStart()
        {
            this.MoveToCardDeployPosition();
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private void MoveToCardDeployPosition()
        {
            this.optimisticDestination = OptimisticDestination.None;
            this.isAtAlpha = false;
            if (this.deskPosition == null) return;
            if (this.deskPosition.CardDeployPosition == null) return;
            this.movement.MoveTo(this.deskPosition.CardDeployPosition);
        }

        private void MoveToAlphaLampPosition()
        {
            this.isAtAlpha = true;
            if (this.deskPosition == null) return;
            if (this.deskPosition.AlphaLampPosition == null) return;
            this.movement.MoveTo(this.deskPosition.AlphaLampPosition);
        }

        private void MoveToOmegaLampPosition()
        {
            this.isAtAlpha = false;
            if (this.deskPosition == null) return;
            if (this.deskPosition.OmegaLampPosition == null) return;
            this.movement.MoveTo(this.deskPosition.OmegaLampPosition);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Moves the lamp to the specified target transform.</summary>
        public void MoveTo(Transform target)
        {
            this.optimisticDestination = OptimisticDestination.None;
            this.movement.MoveTo(target);
        }

        /// <summary>Moves the lamp to the alpha lamp position.</summary>
        public void MoveToAlpha()
        {
            this.optimisticDestination = OptimisticDestination.None;
            this.MoveToAlphaLampPosition();
        }

        /// <summary>Moves the lamp to the omega lamp position.</summary>
        public void MoveToOmega()
        {
            this.optimisticDestination = OptimisticDestination.None;
            this.MoveToOmegaLampPosition();
        }

        public void MoveToAlphaOptimistically()
        {
            this.optimisticDestination = OptimisticDestination.Alpha;
            this.MoveToAlphaLampPosition();
        }

        public void MoveToOmegaOptimistically()
        {
            this.optimisticDestination = OptimisticDestination.Omega;
            this.MoveToOmegaLampPosition();
        }

        public bool TryConsumeOptimisticMoveToAlpha()
        {
            return this.TryConsumeOptimisticMove(OptimisticDestination.Alpha);
        }

        public bool TryConsumeOptimisticMoveToOmega()
        {
            return this.TryConsumeOptimisticMove(OptimisticDestination.Omega);
        }

        public void RollbackOptimisticMove(Vector3 previousPosition, bool wasAtAlpha)
        {
            this.optimisticDestination = OptimisticDestination.None;
            this.isAtAlpha = wasAtAlpha;
            this.movement?.MoveTo(previousPosition);
        }

        private bool TryConsumeOptimisticMove(OptimisticDestination destination)
        {
            if (this.optimisticDestination != destination) return false;
            this.optimisticDestination = OptimisticDestination.None;
            return true;
        }

        /// <summary>Returns the movement component of this lamp.</summary>
        public LampMovement Movement => this.movement;

        /// <summary>True while the lamp is currently animating.</summary>
        public bool IsAnimating => this.movement != null && this.movement.IsAnimating;
    }
}
