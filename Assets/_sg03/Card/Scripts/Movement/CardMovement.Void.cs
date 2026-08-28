using DG.Tweening;
using UnityEngine;

namespace SG03
{
    public partial class CardMovement
    {
        /// <summary>
        /// Moves the card from The Void to the line in a 5-step sequence:
        /// 1. RiseUp: Elevate card Y position.
        /// 2. MoveToHolderKeepY: Move horizontally to holder position.
        /// 3. RotateToHolder: Match holder orientation.
        /// 4. FaceUpKeepY: Flip face-up if required (maintaining elevation).
        /// 5. DescendToHolder: Lower to holder line position.
        /// </summary>
        public void MoveVoidToLine(CardHolderCtrl holder, bool isAlpha, bool isFaceUp, System.Action onReady)
        {
            if (holder == null) return;
            this.KillAllTweens();
            this.isFlipping = false;
            this.ctrl.AssignCardHolder(holder);
            Location destination = holder.HolderLocation;
            this.SetLocation(destination);
            this.RecordHandAnchor(holder.transform, destination);

            // Step 1: Rise up
            this.RiseUp(this.flipRiseHeight, () =>
            {
                // Step 2: Move to holder position maintaining height
                this.MoveToHolderKeepY(holder, () =>
                {
                    // Step 3: AttackDirection - face opponent (Omega -> Alpha, Alpha -> Omega)
                    this.AttackDirection(isAlpha, () =>
                    {
                        if (isFaceUp)
                        {
                            // Step 4: Flip face-up maintaining height and keeping Y direction
                            this.FaceUpKeepY(isAlpha, () =>
                            {
                                // Step 5: Descend to holder
                                this.DescendToHolder(holder, onReady);
                            });
                        }
                        else
                        {
                            // Step 5: Descend to holder
                            this.DescendToHolder(holder, onReady);
                        }
                    });
                });
            });
        }

        public void MoveVoidToLine(CardHolderCtrl holder, bool isFaceUp = true, System.Action onReady = null)
            => this.MoveVoidToLine(holder, holder != null ? holder.HolderOwner == Owner.alpha : true, isFaceUp, onReady);

        public void MoveVoidToLine(CardHolderCtrl holder, Owner owner, bool isFaceUp = true, System.Action onReady = null)
            => this.MoveVoidToLine(holder, owner == Owner.alpha, isFaceUp, onReady);

        /// <summary>
        /// Step 1: Elevates the card by <paramref name="height"/> world units.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void RiseUp(float height, System.Action onComplete = null)
        {
            float targetY = this.transform.position.y + height;
            this.yTween?.Kill();
            this.yTween = this.transform.DOMoveY(targetY, this.duration * 0.5f)
                .SetEase(this.ease)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Step 2: Moves horizontally to <paramref name="holder"/>'s position maintaining current Y height.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void MoveToHolderKeepY(CardHolderCtrl holder, System.Action onComplete = null)
        {
            if (holder == null) return;
            Vector3 targetPos = new Vector3(holder.transform.position.x, this.transform.position.y, holder.transform.position.z);
            this.KillMoveTween();
            this.StartMoveTween(targetPos, this.duration, this.ease, onComplete);
        }

        /// <summary>
        /// Step 3: Rotates the card to face opponent direction (Omega faces Alpha, Alpha faces Omega).
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void AttackDirection(bool isAlpha, System.Action onComplete = null)
        {
            Quaternion targetRotation = isAlpha ? Quaternion.Euler(this.transform.eulerAngles.x, 0f, 0f) : Quaternion.Euler(this.transform.eulerAngles.x, 180f, 0f);
            this.rotateTween?.Kill();
            this.rotateTween = this.transform.DORotateQuaternion(targetRotation, this.duration * 0.5f)
                .SetEase(this.ease)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Step 4: Rotates the card face-up maintaining current Y height and Y attack direction.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void FaceUpKeepY(bool isAlpha, System.Action onComplete = null)
        {
            this.faceState = FaceState.FaceUp;
            this.isFlipping = true;
            this.faceTween?.Kill();
            this.faceTween = DOTween.Sequence();
            float targetYAngle = isAlpha ? 0f : 180f;
            Vector3 targetRotation = new Vector3(this.faceUpRotation.x, targetYAngle, this.faceUpRotation.z);
            this.faceTween.Append(
                this.transform.DORotateQuaternion(Quaternion.Euler(targetRotation), this.flipDuration * 2f)
                    .SetEase(this.flipEase));
            this.faceTween.OnComplete(() =>
            {
                this.isFlipping = false;
                onComplete?.Invoke();
            });
            this.faceTween.OnKill(() => this.isFlipping = false);
        }

        /// <summary>
        /// Step 5: Descends the card into final <paramref name="holder"/> line position.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void DescendToHolder(CardHolderCtrl holder, System.Action onComplete = null)
        {
            if (holder == null) return;
            Vector3 finalPos = holder.transform.position + new Vector3(0f, this.lineOffsetY, 0f);
            this.KillMoveTween();
            this.StartMoveTween(finalPos, this.duration * 0.5f, this.ease, onComplete);
        }
    }
}

