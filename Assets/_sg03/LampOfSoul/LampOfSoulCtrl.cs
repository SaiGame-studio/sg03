using SaiGame.Services;
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

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadLampMovement();
        }

        protected virtual void LoadLampMovement()
        {
            if (this.movement != null) return;
            this.movement = this.GetComponent<LampMovement>();
            Debug.LogWarning(this.transform.name + ": LoadLampMovement", this.gameObject);
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Moves the lamp to the specified target transform.</summary>
        public void MoveTo(Transform target) => this.movement.MoveTo(target);

        /// <summary>Returns the movement component of this lamp.</summary>
        public LampMovement Movement => this.movement;
    }
}
