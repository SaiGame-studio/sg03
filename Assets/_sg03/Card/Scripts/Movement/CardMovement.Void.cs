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
            if (holder == null)
            {
                this.LogMoveVoidToLine("ABORT holder=null", null);
                return;
            }
            this.KillAllTweens();
            this.isVoidToLineTransitionActive = true;
            this.isFlipping = false;
            this.ctrl.AssignCardHolder(holder);
            Location destination = holder.HolderLocation;
            this.SetLocation(destination);
            this.RecordHandAnchor(holder.transform, destination);
            this.LogMoveVoidToLine(
                "START owner=" + (isAlpha ? "alpha" : "omega") + ",faceUp=" + isFaceUp,
                holder);

            // Step 1: Rise up
            this.RiseUp(holder, this.flipRiseHeight, () =>
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
                                this.DescendToHolder(holder, () => this.CompleteVoidToLine(holder, onReady));
                            });
                        }
                        else
                        {
                            this.LogMoveVoidToLine("STEP 4 SKIPPED FaceUpKeepY", holder);
                            // Step 5: Descend to holder
                            this.DescendToHolder(holder, () => this.CompleteVoidToLine(holder, onReady));
                        }
                    });
                });
            });
        }

        public void MoveVoidToLine(CardHolderCtrl holder, bool isFaceUp = true, System.Action onReady = null)
            => this.MoveVoidToLine(holder, holder != null ? holder.HolderOwner == Owner.alpha : true, isFaceUp, onReady);

        public void MoveVoidToLine(CardHolderCtrl holder, Owner owner, bool isFaceUp = true, System.Action onReady = null)
            => this.MoveVoidToLine(holder, owner == Owner.alpha, isFaceUp, onReady);

        private void CompleteVoidToLine(CardHolderCtrl holder, System.Action onReady)
        {
            holder?.RefreshHeldCardPlacement();
            this.isVoidToLineTransitionActive = false;
            this.LogMoveVoidToLine("COMPLETE", holder);
            onReady?.Invoke();
        }

        private void LogMoveVoidToLine(string message, CardHolderCtrl holder)
        {
            if (!this.debugMoveVoidToLine) return;
            holder?.RefreshHeldCardPlacement();
            string holderName = holder != null ? holder.name : "null";
            string holderPosition = holder != null ? holder.transform.position.ToString("F3") : "null";
            string heldCardName = holder != null && holder.HeldCard != null ? holder.HeldCard.name : "null";
            string heldCardDistance = holder != null ? holder.HeldCardDistance.ToString("F4") : "n/a";
            string movingCardDistance = holder != null
                ? Vector3.Distance(holder.transform.position, this.transform.position).ToString("F4")
                : "n/a";
            string heldCardMatches = holder != null ? (holder.HeldCard == this.ctrl).ToString() : "n/a";
            string isOnHolder = holder != null ? holder.IsHeldCardOnHolder.ToString() : "n/a";
            Debug.Log(
                "[MoveVoidToLine] " + message +
                " | card=" + this.gameObject.name +
                " cardPos=" + this.transform.position.ToString("F3") +
                " holder=" + holderName +
                " holderPos=" + holderPosition +
                " heldCard=" + heldCardName +
                " heldCardMatches=" + heldCardMatches +
                " heldCardDistance=" + heldCardDistance +
                " movingCardDistance=" + movingCardDistance +
                " isOnHolder=" + isOnHolder +
                " isAnimating=" + this.IsAnimating,
                this.gameObject);
        }

        /// <summary>
        /// Step 1: Elevates the card by <paramref name="height"/> world units.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void RiseUp(CardHolderCtrl holder, float height, System.Action onComplete = null)
        {
            float targetY = this.transform.position.y + height;
            this.LogMoveVoidToLine("STEP 1 START RiseUp targetY=" + targetY.ToString("F3"), holder);
            this.yTween?.Kill();
            bool completed = false;
            this.yTween = this.transform.DOMoveY(targetY, this.duration * 0.5f)
                .SetEase(this.ease)
                .OnComplete(() =>
                {
                    completed = true;
                    this.LogMoveVoidToLine("STEP 1 COMPLETE RiseUp", holder);
                    onComplete?.Invoke();
                })
                .OnKill(() =>
                {
                    if (!completed) this.LogMoveVoidToLine("STEP 1 INTERRUPTED RiseUp", holder);
                });
        }

        /// <summary>
        /// Step 2: Moves horizontally to <paramref name="holder"/>'s position maintaining current Y height.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void MoveToHolderKeepY(CardHolderCtrl holder, System.Action onComplete = null)
        {
            if (holder == null) return;
            Vector3 targetPos = new Vector3(holder.transform.position.x, this.transform.position.y, holder.transform.position.z);
            this.LogMoveVoidToLine("STEP 2 START MoveToHolderKeepY target=" + targetPos.ToString("F3"), holder);
            this.KillMoveTween();
            this.StartMoveTween(targetPos, this.duration, this.ease, () =>
            {
                this.LogMoveVoidToLine("STEP 2 COMPLETE MoveToHolderKeepY", holder);
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Step 3: Rotates the card 90 degrees from its Void orientation to face the opponent.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void AttackDirection(bool isAlpha, System.Action onComplete = null)
        {
            float zRotation = isAlpha ? -90f : 90f;
            CardHolderCtrl holder = this.ctrl != null ? this.ctrl.CardHolder : null;
            this.LogMoveVoidToLine("STEP 3 START AttackDirection z=" + zRotation.ToString("F1"), holder);
            this.rotateTween?.Kill();
            bool completed = false;
            this.rotateTween = this.transform.DORotate(
                    new Vector3(0f, 0f, zRotation),
                    this.duration * 0.5f,
                    RotateMode.WorldAxisAdd)
                .SetEase(this.ease)
                .OnComplete(() =>
                {
                    completed = true;
                    this.LogMoveVoidToLine("STEP 3 COMPLETE AttackDirection", holder);
                    onComplete?.Invoke();
                })
                .OnKill(() =>
                {
                    if (!completed) this.LogMoveVoidToLine("STEP 3 INTERRUPTED AttackDirection", holder);
                });
        }

        /// <summary>
        /// Step 4: Rotates the card face-up while maintaining its current elevation.
        /// Omega cards also rotate 180 degrees around world Z so their face-up artwork points toward Alpha.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void FaceUpKeepY(bool isAlpha, System.Action onComplete = null)
        {
            CardHolderCtrl holder = this.ctrl != null ? this.ctrl.CardHolder : null;
            this.LogMoveVoidToLine("STEP 4 START FaceUpKeepY", holder);
            this.faceState = FaceState.FaceUp;
            this.isFlipping = true;
            this.faceTween?.Kill();
            this.faceTween = DOTween.Sequence();
            float targetZAngle = this.faceUpRotation.z + (isAlpha ? 0f : 180f);
            Vector3 targetRotation = new Vector3(this.faceUpRotation.x, this.faceUpRotation.y, targetZAngle);
            this.faceTween.Append(
                this.transform.DORotateQuaternion(Quaternion.Euler(targetRotation), this.flipDuration * 2f)
                    .SetEase(this.flipEase));
            this.faceTween.OnComplete(() =>
            {
                this.isFlipping = false;
                this.LogMoveVoidToLine("STEP 4 COMPLETE FaceUpKeepY", holder);
                onComplete?.Invoke();
            });
            this.faceTween.OnKill(() =>
            {
                bool wasFlipping = this.isFlipping;
                this.isFlipping = false;
                if (wasFlipping) this.LogMoveVoidToLine("STEP 4 INTERRUPTED FaceUpKeepY", holder);
            });
        }

        /// <summary>
        /// Step 5: Descends the card vertically into final <paramref name="holder"/> line position without any rotation.
        /// Dedicated helper step for <see cref="MoveVoidToLine"/> sequence. Do NOT share with other movement paths.
        /// </summary>
        private void DescendToHolder(CardHolderCtrl holder, System.Action onComplete = null)
        {
            if (holder == null) return;
            this.rotateTween?.Kill();
            this.rotateTween = null;
            float finalY = holder.transform.position.y + this.lineOffsetY;
            this.LogMoveVoidToLine("STEP 5 START DescendToHolder targetY=" + finalY.ToString("F3"), holder);
            this.yTween?.Kill();
            bool completed = false;
            this.yTween = this.transform.DOMoveY(finalY, this.duration * 0.5f)
                .SetEase(this.ease)
                .OnComplete(() =>
                {
                    completed = true;
                    this.LogMoveVoidToLine("STEP 5 COMPLETE DescendToHolder", holder);
                    onComplete?.Invoke();
                })
                .OnKill(() =>
                {
                    if (!completed) this.LogMoveVoidToLine("STEP 5 INTERRUPTED DescendToHolder", holder);
                });
        }
    }
}
