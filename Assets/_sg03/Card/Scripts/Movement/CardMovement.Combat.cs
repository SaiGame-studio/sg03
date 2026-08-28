using DG.Tweening;
using UnityEngine;

namespace SG03
{
    public partial class CardMovement
    {
        public void RunUp()
        {
            if (this.isVoidToLineTransitionActive) return;
            Vector3 origin = this.transform.position;
            Vector3 risen = origin + Vector3.up * this.damagedRiseHeight;
            this.damageTween?.Kill();
            this.damageTween = DOTween.Sequence();
            this.damageTween.Append(this.transform.DOMove(risen, this.damagedPhaseDuration).SetEase(this.damagedRiseEase));
            this.damageTween.Append(this.transform.DOMove(origin, this.damagedPhaseDuration).SetEase(this.damagedFallEase));
        }

        public void Damaged()
        {
            if (this.isVoidToLineTransitionActive) return;
            this.damageTween?.Kill();
            this.damageTween = DOTween.Sequence();
            this.damageTween.Append(this.transform.DOShakePosition(this.damagedPhaseDuration * 2f, new Vector3(0, 0, 0.5f), 20, 90f, false, true));
        }

        public void AbilityActive()
        {
            if (this.isVoidToLineTransitionActive) return;
            Vector3 origin = this.transform.position;
            Vector3 risen = origin + Vector3.up * this.damagedRiseHeight;
            this.damageTween?.Kill();
            this.damageTween = DOTween.Sequence();
            this.damageTween.Append(this.transform.DOMove(risen, this.damagedPhaseDuration).SetEase(Ease.OutQuad));
            this.damageTween.Append(this.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.3f, 5, 0.5f));
            this.damageTween.Append(this.transform.DOMove(origin, this.damagedPhaseDuration).SetEase(Ease.InQuad));
        }

        public void AttackLunge(Vector3 defenderPosition)
        {
            if (this.isVoidToLineTransitionActive) return;
            Vector3 origin = this.transform.position;
            Vector3 returnPosition = this.GetAttackReturnPosition(origin);
            Vector3 lunged = Vector3.Lerp(origin, defenderPosition, this.attackLungeRatio);
            this.attackTween?.Kill();
            this.attackTween = DOTween.Sequence();
            this.attackTween.Append(this.transform.DOMove(lunged, this.attackLungeDuration).SetEase(this.attackLungeEase));
            this.attackTween.Append(this.transform.DOMove(returnPosition, this.attackReturnDuration).SetEase(this.attackReturnEase));
        }

        /// <summary>
        /// Plays the attack animation with a small backstep first: card pulls back slightly away from the defender,
        /// then lunges forward into the defender, then returns to its origin.
        /// </summary>
        public void AttackBackstepLunge(Vector3 defenderPosition)
        {
            if (this.isVoidToLineTransitionActive) return;
            Vector3 origin = this.transform.position;
            Vector3 returnPosition = this.GetAttackReturnPosition(origin);
            Vector3 toDefender = defenderPosition - origin;
            Vector3 backstep = origin - toDefender * this.attackBackstepRatio;
            Vector3 lunged = Vector3.Lerp(origin, defenderPosition, this.attackLungeRatio);
            this.attackTween?.Kill();
            this.attackTween = DOTween.Sequence();
            this.attackTween.Append(this.transform.DOMove(backstep, this.attackBackstepDuration).SetEase(this.attackBackstepEase));
            this.attackTween.Append(this.transform.DOMove(lunged, this.attackLungeDuration).SetEase(this.attackLungeEase));
            this.attackTween.Append(this.transform.DOMove(returnPosition, this.attackReturnDuration).SetEase(this.attackReturnEase));
        }

        /// <summary>
        /// Moves the card forward toward the defender and stops exactly
        /// <see cref="planningStopDistance"/> world units away from the defender.
        /// No return tween — the card stays there waiting for alpha's response.
        /// </summary>
        public void PlanningLunge(Vector3 defenderPosition)
        {
            if (this.isVoidToLineTransitionActive) return;
            Vector3 direction = (defenderPosition - this.transform.position).normalized;
            Vector3 lunged = defenderPosition - direction * this.planningStopDistance;
            this.attackTween?.Kill();
            this.attackTween = DOTween.Sequence();
            this.attackTween.Append(this.transform.DOMove(lunged, this.attackLungeDuration).SetEase(this.attackLungeEase));
        }

        /// <summary>
        /// Moves the card directly to the given destination position (no stop-distance offset).
        /// Used when the caller has already computed the exact world position to stop at.
        /// </summary>
        public void PlanningLungeTo(Vector3 destination)
        {
            if (this.isVoidToLineTransitionActive) return;
            this.faceTween?.Kill();
            this.attackTween?.Kill();
            this.attackTween = DOTween.Sequence();
            this.attackTween.Append(this.transform.DOMove(destination, this.attackLungeDuration).SetEase(this.attackLungeEase));
        }

        /// <summary>
        /// Plays the ability activation animation using the same lightweight rise/fall motion as damage.
        /// </summary>
        public void ActivateAbility() => this.RunUp();

        private Vector3 GetAttackReturnPosition(Vector3 fallbackPosition)
        {
            CardHolderCtrl holder = this.ctrl != null ? this.ctrl.CardHolder : null;
            if (holder == null) return fallbackPosition;
            return holder.transform.position + new Vector3(0f, this.lineOffsetY, 0f);
        }
    }
}
