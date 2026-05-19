using DG.Tweening;
using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Handles physical movement of a card in world space.
    /// Attach alongside <see cref="Card3DCtrl"/> on the same GameObject.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Movement")]
    [RequireComponent(typeof(Card3DCtrl))]
    public class CardMovement : SaiBehaviour
    {
        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private Card3DCtrl ctrl;

        // ─── Move animation ───────────────────────────────────────────────────────

        [Header("Move Animation")]
        [Tooltip("Duration of the move-to animation in seconds.")]
        [SerializeField] private float duration = 1f;

        [Tooltip("Ease curve applied to the move-to animation.")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Tooltip("Y offset (world units) applied when placing a card into a line slot.")]
        [SerializeField] private float lineOffsetY = 0.3f;

        // ─── In-Hand Hover ────────────────────────────────────────────────────────

        [Header("In-Hand Hover")]
        [Tooltip("Y offset (world units) the card rises when hovered while in hand.")]
        [SerializeField] private float hoverOffsetY = 0.3f;

        [Tooltip("Duration of the hover rise/fall animation in seconds.")]
        [SerializeField] private float hoverDuration = 0.15f;

        [Tooltip("Ease curve for the hover animation.")]
        [SerializeField] private Ease hoverEase = Ease.OutQuad;

        // ─── In-Hand Selected ─────────────────────────────────────────────────────

        [Header("In-Hand Selected")]
        [Tooltip("Y offset (world units) the card rises when selected while in hand.")]
        [SerializeField] private float selectedOffsetY = 1.5f;

        [Tooltip("Duration of the select rise animation in seconds.")]
        [SerializeField] private float selectedDuration = 0.25f;

        [Tooltip("Ease curve for the select animation.")]
        [SerializeField] private Ease selectedEase = Ease.OutBack;

        // ─── Full Detail ──────────────────────────────────────────────────────────

        [Header("Full Detail")]
        [Tooltip("Duration of the move to FullDetailPoint in seconds.")]
        [SerializeField] private float fullDetailDuration = 0.5f;

        [Tooltip("Ease curve for moving to FullDetailPoint.")]
        [SerializeField] private Ease fullDetailEase = Ease.OutQuad;

        [Tooltip("Duration of the return from FullDetailPoint in seconds.")]
        [SerializeField] private float fullDetailReturnDuration = 0.4f;

        [Tooltip("Ease curve for returning from FullDetailPoint.")]
        [SerializeField] private Ease fullDetailReturnEase = Ease.InOutQuad;

        // ─── Face Rotation ────────────────────────────────────────────────────────

        [Header("Face Rotation")]
        [Tooltip("World-space euler angles when the card is face-up.")]
        [SerializeField] private Vector3 faceUpRotation = new Vector3(90f, 0f, 0f);

        [Tooltip("World-space euler angles when the card is face-down.")]
        [SerializeField] private Vector3 faceDownRotation = new Vector3(-90f, 0f, 0f);

        [Tooltip("World-space axis used when flipping between FaceUp and FaceDown.")]
        [SerializeField] private Vector3 flipAxisUpDown = new Vector3(0f, 0f, 1f);

        [Tooltip("World units the card rises during the flip.")]
        [SerializeField] private float flipRiseHeight = 5f;

        [Tooltip("Duration of each flip phase in seconds.")]
        [SerializeField] private float flipDuration = 0.4f;

        [Tooltip("Ease curve for the flip.")]
        [SerializeField] private Ease flipEase = Ease.InOutQuad;

        [Tooltip("Duration of the Y-axis 180 rotation when a card is placed into a line.")]
        [SerializeField] private float rotateY180Duration = 0.4f;

        [Tooltip("Ease curve for the Y-axis 180 rotation.")]
        [SerializeField] private Ease rotateY180Ease = Ease.InOutQuad;

        // ─── Damaged ──────────────────────────────────────────────────────────────

        [Header("Damaged")]
        [Tooltip("World units the card rises when taking damage.")]
        [SerializeField] private float damagedRiseHeight = 1.5f;

        [Tooltip("Duration of each phase (rise + fall) of the damage animation.")]
        [SerializeField] private float damagedPhaseDuration = 0.15f;

        [Tooltip("Ease for the rise phase of the damage animation.")]
        [SerializeField] private Ease damagedRiseEase = Ease.OutQuad;

        [Tooltip("Ease for the fall phase of the damage animation.")]
        [SerializeField] private Ease damagedFallEase = Ease.InQuad;

        // ─── Attack ───────────────────────────────────────────────────────────────

        [Header("Attack")]
        [Tooltip("Fraction (0-1) of the distance toward the defender the attacker travels.")]
        [SerializeField] private float attackLungeRatio = 0.4f;

        [Tooltip("Duration of the lunge-forward phase in seconds.")]
        [SerializeField] private float attackLungeDuration = 0.15f;

        [Tooltip("Duration of the return phase in seconds.")]
        [SerializeField] private float attackReturnDuration = 0.25f;

        [Tooltip("Ease for the lunge-forward phase.")]
        [SerializeField] private Ease attackLungeEase = Ease.OutQuad;

        [Tooltip("Ease for the return phase.")]
        [SerializeField] private Ease attackReturnEase = Ease.InQuad;

        [Tooltip("World-space distance from the defender where PlanningLunge stops.")]
        [SerializeField] private float planningStopDistance = 7f;

        [Tooltip("Fraction (0-1) of the lunge distance the attacker steps backward away from the defender before lunging.")]
        [SerializeField] private float attackBackstepRatio = 0.15f;

        [Tooltip("Duration of the backstep phase in seconds.")]
        [SerializeField] private float attackBackstepDuration = 0.12f;

        [Tooltip("Ease for the backstep phase.")]
        [SerializeField] private Ease attackBackstepEase = Ease.OutQuad;

        // ─── Ability ──────────────────────────────────────────────────────────────

        [Header("Ability")]
        [Tooltip("World units the card rises while activating its ability.")]
        [SerializeField] private float abilityRiseHeight = 1.2f;

        [Tooltip("Extra scale multiplier applied at the peak of the ability animation (1 = no change).")]
        [SerializeField] private float abilityScalePeak = 1.15f;

        [Tooltip("Duration of the rise/scale-up phase in seconds.")]
        [SerializeField] private float abilityRiseDuration = 0.18f;

        [Tooltip("Duration of the hold phase at the peak in seconds.")]
        [SerializeField] private float abilityHoldDuration = 0.12f;

        [Tooltip("Duration of the return phase in seconds.")]
        [SerializeField] private float abilityReturnDuration = 0.22f;

        [Tooltip("Ease for the rise/scale-up phase.")]
        [SerializeField] private Ease abilityRiseEase = Ease.OutBack;

        [Tooltip("Ease for the return phase.")]
        [SerializeField] private Ease abilityReturnEase = Ease.InQuad;

        // ─── Runtime state ────────────────────────────────────────────────────────

        [Header("State")]
        [SerializeField] private Location location;
        [SerializeField] private FaceState faceState = FaceState.Unknown;

        private float handAnchorY;
        private bool  isSelected;
        private bool  isFlipping;
        private Tween yTween;
        private Tween moveTween;
        private Tween rotateTween;
        private Tween rotateY180Tween;
        private Sequence faceTween;
        private Sequence damageTween;
        private Sequence attackTween;
        private Sequence abilityTween;
        private Vector3    preFullDetailPosition;
        private Quaternion preFullDetailRotation;

        // ─── Static movement gate ─────────────────────────────────────────────────

        private static int movingCount = 0;
        public  static bool IsAnyCardMoving => movingCount > 0;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCard3DCtrl();
        }

        protected virtual void LoadCard3DCtrl()
        {
            if (this.ctrl != null) return;
            this.ctrl = this.GetComponent<Card3DCtrl>();
            Debug.LogWarning(transform.name + ": LoadCard3DCtrl", gameObject);
        }

        private void OnEnable()  => this.Subscribe();
        private void OnDisable() => this.Unsubscribe();
        private void OnDestroy() => this.KillAllTweens();

        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered += this.OnHoverEntered;
            Card3DCtrl.HoverExited  += this.OnHoverExited;
            Card3DCtrl.CardSelected += this.OnCardSelected;
        }

        private void Unsubscribe()
        {
            Card3DCtrl.HoverEntered -= this.OnHoverEntered;
            Card3DCtrl.HoverExited  -= this.OnHoverExited;
            Card3DCtrl.CardSelected -= this.OnCardSelected;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        public Location  Location   => this.location;
        public bool      IsFlipping  => this.isFlipping;
        public bool      IsAnimating =>
            (this.moveTween != null && this.moveTween.IsActive()) ||
            this.isFlipping ||
            (this.damageTween != null && this.damageTween.IsActive()) ||
            (this.attackTween != null && this.attackTween.IsActive()) ||
            (this.abilityTween != null && this.abilityTween.IsActive());
        public FaceState FaceState   => this.faceState;

        public void SetMoveDuration(float d)   { this.duration          = d; }
        public void SetRotateDuration(float d)  { this.rotateY180Duration = d; }

        /// <summary>
        /// Smoothly moves the card to the specified world-space <paramref name="target"/> position.
        /// Any in-progress tween is cancelled before starting the new one.
        /// </summary>
        public void MoveAndRotate(Transform target, Location destination)
        {
            if (this.isFlipping)
            {
                Debug.LogWarning($"[CardMovement] {this.gameObject.name} MoveAndRotate SKIPPED — isFlipping=true");
                return;
            }
            this.location = destination;
            this.RecordHandAnchor(target, destination);
            this.KillAllTweens();
            this.StartMoveTween(target.position, this.duration, this.ease, null);
            this.rotateTween = this.transform.DORotateQuaternion(target.rotation, this.duration).SetEase(this.ease);
        }

        /// <summary>
        /// Smoothly moves the card to the specified world-space <paramref name="target"/> position
        /// without changing its rotation. Any in-progress tween is cancelled before starting the new one.
        /// </summary>
        public void MoveTo(Transform target, Location destination)
        {
            if (this.isFlipping) return;
            this.location = destination;
            this.RecordHandAnchor(target, destination);
            this.KillAllTweens();
            this.StartMoveTween(target.position, this.duration, this.ease, null);
        }

        /// <summary>
        /// Smoothly moves the card to the specified world-space <paramref name="target"/> position
        /// without changing its rotation, then invokes <paramref name="onComplete"/> when the move finishes.
        /// </summary>
        public void MoveTo(Transform target, Location destination, System.Action onComplete)
        {
            if (this.isFlipping) return;
            this.location = destination;
            this.RecordHandAnchor(target, destination);
            this.KillAllTweens();
            this.StartMoveTween(target.position, this.duration, this.ease, onComplete);
        }

        /// <summary>
        /// Moves the card to <paramref name="holder"/>'s position and simultaneously flips
        /// face-down using the Unknown axis. Intended for hand → line transitions.
        /// </summary>
        public void MoveToUnknow(CardHolderCtrl holder, System.Action onReady = null)
        {
            if (this.isFlipping) return;
            if (holder == null) return;
            // Debug.Log($"[CardMovement] {this.gameObject.name} MoveToUnknow → '{holder.name}' (moveTween active: {this.moveTween != null && this.moveTween.IsActive()})");
            this.ctrl.AssignCardHolder(holder);
            Location destination = holder.HolderLocation;
            this.location = destination;
            this.RecordHandAnchor(holder.transform, destination);
            this.KillAllTweens();
            Vector3 lineDestination = holder.transform.position + new Vector3(0f, this.lineOffsetY, 0f);
            this.StartMoveTween(lineDestination, this.duration, this.ease, onReady);
            this.FaceDownUnknown();
        }

        public void MoveBackToLineHolder(CardHolderCtrl holder)
        {
            if (holder == null) return;
            this.location = holder.HolderLocation;
            this.RecordHandAnchor(holder.transform, this.location);
            this.KillAllTweens();
            Vector3 lineDestination = holder.transform.position + new Vector3(0f, this.lineOffsetY, 0f);
            this.StartMoveTween(lineDestination, this.duration, this.ease, null);
        }

        /// <summary>Smoothly rotates the card to face-up using global euler angles.</summary>
        public void FaceUp()
        {
            if (this.isFlipping) return;
            this.faceState = FaceState.FaceUp;
            this.DoFaceFlip(this.faceUpRotation, this.flipAxisUpDown);
        }

        /// <summary>Smoothly rotates the card to face-down using global euler angles.</summary>
        public void FaceDown()
        {
            if (this.isFlipping) return;
            this.faceState = FaceState.FaceDown;
            this.DoFaceFlip(this.faceDownRotation, this.flipAxisUpDown);
        }

        /// <summary>Rotates the card to face-up using the Unknown axis, without rising.</summary>
        public void FaceUpUnknown()
        {
            if (this.isFlipping) return;
            this.faceState = FaceState.FaceUp;
            this.DoFaceFlipNoRise(this.faceUpRotation);
        }

        /// <summary>Rotates the card to face-down using the Unknown axis, without rising.</summary>
        public void FaceDownUnknown()
        {
            if (this.isFlipping) return;
            this.faceState = FaceState.FaceDown;
            this.DoFaceFlipNoRise(this.faceDownRotation);
        }

        /// <summary>Toggles between FaceUp and FaceDown. Defaults to FaceUp when Unknown.</summary>
        public void ToggleFace()
        {
            if (this.faceState == FaceState.FaceUp)
            {
                this.FaceDown();
                return;
            }
            this.FaceUp();
        }

        /// <summary>Rotates the card 180 degrees around the world Y axis, then invokes <paramref name="onComplete"/>.</summary>
        public void RotateY180(System.Action onComplete = null)
        {
            if (this.isFlipping) return;
            this.isFlipping = true;
            this.rotateY180Tween?.Kill();
            this.rotateY180Tween = this.transform.DORotate(new Vector3(0f, 180f, 0f), this.rotateY180Duration, RotateMode.WorldAxisAdd)
                .SetEase(this.rotateY180Ease)
                .OnComplete(() => { this.isFlipping = false; onComplete?.Invoke(); })
                .OnKill(() => this.isFlipping = false);
        }

        // Rotates the card directly to the global target orientation without rising.
        // Uses DORotateQuaternion so the card always lands at the correct world-space
        // rotation regardless of the card's starting orientation (e.g. Omega cards with Y rotation).
        private void DoFaceFlipNoRise(Vector3 targetEulers)
        {
            float totalTime = this.flipDuration * 2f;

            this.isFlipping = true;
            this.faceTween?.Kill();
            this.faceTween = DOTween.Sequence();
            this.faceTween.Insert(0f,
                this.transform.DORotateQuaternion(Quaternion.Euler(targetEulers), totalTime)
                    .SetEase(this.flipEase));
            this.faceTween.OnComplete(() => this.isFlipping = false);
            this.faceTween.OnKill(() => this.isFlipping = false);
        }

        private void DoFaceFlip(Vector3 targetEulers, Vector3 axis)
        {
            Vector3 origin    = this.transform.position;
            Vector3 risen     = origin + Vector3.up * this.flipRiseHeight;
            float   totalTime = this.flipDuration * 2f;
            float   angle     = this.ComputeFlipAngle(targetEulers, axis);

            this.isFlipping = true;
            this.faceTween?.Kill();
            this.faceTween = DOTween.Sequence();
            this.faceTween.OnKill(() => this.isFlipping = false);
            // Rise
            this.faceTween.Append(
                this.transform.DOMove(risen, this.flipDuration).SetEase(this.flipEase));
            // Rotate around the selected axis to reach target orientation (starts simultaneously with rise)
            this.faceTween.Insert(0f,
                this.transform.DORotate(axis.normalized * angle, totalTime, RotateMode.WorldAxisAdd)
                    .SetEase(this.flipEase));
            // Descend back to origin
            this.faceTween.Append(
                this.transform.DOMove(origin, this.flipDuration).SetEase(this.flipEase));
        }

        private float ComputeFlipAngle(Vector3 targetEulers, Vector3 axis)
        {
            Quaternion from  = this.transform.rotation;
            Quaternion to    = Quaternion.Euler(targetEulers);
            Quaternion delta = to * Quaternion.Inverse(from);
            delta.ToAngleAxis(out float angle, out Vector3 rotAxis);
            return angle * Mathf.Sign(Vector3.Dot(rotAxis, axis.normalized));
        }

        /// <summary>
        /// Smoothly moves the card to <paramref name="point"/> without changing location or hand anchor.
        /// Used when the player double-clicks a selected in-hand card to view full detail.
        /// </summary>
        public void MoveToFullDetail(Transform point)
        {
            if (this.isFlipping) return;
            this.preFullDetailPosition = this.transform.position;
            this.preFullDetailRotation = this.transform.rotation;
            this.KillAllTweens();
            this.StartMoveTween(point.position, this.fullDetailDuration, this.fullDetailEase, null);
            this.rotateTween = this.transform.DORotateQuaternion(point.rotation, this.fullDetailDuration).SetEase(this.fullDetailEase);
        }

        /// <summary>
        /// Returns the card from full-detail back to the position it occupied just before entering full detail.
        /// </summary>
        public void ReturnFromFullDetail()
        {
            if (this.isFlipping) return;
            this.KillAllTweens();
            this.StartMoveTween(this.preFullDetailPosition, this.fullDetailReturnDuration, this.fullDetailReturnEase, null);
            this.rotateTween = this.transform.DORotateQuaternion(this.preFullDetailRotation, this.fullDetailReturnDuration).SetEase(this.fullDetailReturnEase);
        }

        public void RunUp()
        {
            Vector3 origin = this.transform.position;
            Vector3 risen  = origin + Vector3.up * this.damagedRiseHeight;
            this.damageTween?.Kill();
            this.damageTween = DOTween.Sequence();
            this.damageTween.Append(this.transform.DOMove(risen,   this.damagedPhaseDuration).SetEase(this.damagedRiseEase));
            this.damageTween.Append(this.transform.DOMove(origin,  this.damagedPhaseDuration).SetEase(this.damagedFallEase));
        }

        public void AttackLunge(Vector3 defenderPosition)
        {
            Vector3 origin = this.transform.position;
            Vector3 lunged = Vector3.Lerp(origin, defenderPosition, this.attackLungeRatio);
            this.attackTween?.Kill();
            this.attackTween = DOTween.Sequence();
            this.attackTween.Append(this.transform.DOMove(lunged, this.attackLungeDuration).SetEase(this.attackLungeEase));
            this.attackTween.Append(this.transform.DOMove(origin, this.attackReturnDuration).SetEase(this.attackReturnEase));
        }

        /// <summary>
        /// Plays the attack animation with a small backstep first: card pulls back slightly away from the defender,
        /// then lunges forward into the defender, then returns to its origin.
        /// </summary>
        public void AttackBackstepLunge(Vector3 defenderPosition)
        {
            Vector3 origin     = this.transform.position;
            Vector3 toDefender = defenderPosition - origin;
            Vector3 backstep   = origin - toDefender * this.attackBackstepRatio;
            Vector3 lunged     = Vector3.Lerp(origin, defenderPosition, this.attackLungeRatio);
            this.attackTween?.Kill();
            this.attackTween = DOTween.Sequence();
            this.attackTween.Append(this.transform.DOMove(backstep, this.attackBackstepDuration).SetEase(this.attackBackstepEase));
            this.attackTween.Append(this.transform.DOMove(lunged,   this.attackLungeDuration).SetEase(this.attackLungeEase));
            this.attackTween.Append(this.transform.DOMove(origin,   this.attackReturnDuration).SetEase(this.attackReturnEase));
        }

        /// <summary>
        /// Moves the card forward toward the defender and stops exactly
        /// <see cref="planningStopDistance"/> world units away from the defender.
        /// No return tween — the card stays there waiting for alpha's response.
        /// </summary>
        public void PlanningLunge(Vector3 defenderPosition)
        {
            Vector3 direction = (defenderPosition - this.transform.position).normalized;
            Vector3 lunged    = defenderPosition - direction * this.planningStopDistance;
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
            this.attackTween?.Kill();
            this.attackTween = DOTween.Sequence();
            this.attackTween.Append(this.transform.DOMove(destination, this.attackLungeDuration).SetEase(this.attackLungeEase));
        }

        /// <summary>
        /// Plays the ability activation animation: card rises and scales up, holds briefly, then returns.
        /// </summary>
        public void ActivateAbility()
        {
            Vector3 origin     = this.transform.position;
            Vector3 risen      = origin + Vector3.up * this.abilityRiseHeight;
            Vector3 baseScale  = this.transform.localScale;
            Vector3 peakScale  = baseScale * this.abilityScalePeak;
            this.abilityTween?.Kill();
            this.abilityTween = DOTween.Sequence();
            this.abilityTween.Append(this.transform.DOMove(risen, this.abilityRiseDuration).SetEase(this.abilityRiseEase));
            this.abilityTween.Join(this.transform.DOScale(peakScale, this.abilityRiseDuration).SetEase(this.abilityRiseEase));
            this.abilityTween.AppendInterval(this.abilityHoldDuration);
            this.abilityTween.Append(this.transform.DOMove(origin, this.abilityReturnDuration).SetEase(this.abilityReturnEase));
            this.abilityTween.Join(this.transform.DOScale(baseScale, this.abilityReturnDuration).SetEase(this.abilityReturnEase));
        }

        // ─── Hover handlers ───────────────────────────────────────────────────────

        private void OnHoverEntered(Card3DCtrl card)
        {
            if (card != this.ctrl) return;
            if (this.location != Location.in_hand) return;
            if (this.isSelected) return;
            this.TweenToY(this.handAnchorY + this.hoverOffsetY, this.hoverDuration, this.hoverEase);
        }

        private void OnHoverExited(Card3DCtrl card)
        {
            if (card != this.ctrl) return;
            if (this.location != Location.in_hand) return;
            if (this.isSelected) return;
            this.TweenToY(this.handAnchorY, this.hoverDuration, this.hoverEase);
        }

        // ─── Select handler ───────────────────────────────────────────────────────

        private void OnCardSelected(Card3DCtrl card)
        {
            if (this.location != Location.in_hand) return;
            if (card == this.ctrl)
            {
                this.SelectSelf();
                return;
            }
            this.DeselectSelf();
        }

        private void SelectSelf()
        {
            this.isSelected = true;
            this.TweenToY(this.handAnchorY + this.selectedOffsetY, this.selectedDuration, this.selectedEase);
        }

        private void DeselectSelf()
        {
            if (!this.isSelected) return;
            this.isSelected = false;
            this.TweenToY(this.handAnchorY, this.hoverDuration, this.hoverEase);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void RecordHandAnchor(Transform target, Location destination)
        {
            if (destination != Location.in_hand) return;
            this.handAnchorY = target.position.y;
            this.isSelected  = false;
        }

        private void TweenToY(float targetY, float dur, Ease easeType)
        {
            this.yTween?.Kill();
            this.yTween = this.transform.DOMoveY(targetY, dur).SetEase(easeType);
        }

        private void StartMoveTween(Vector3 target, float dur, Ease easeType, System.Action onComplete)
        {
            movingCount++;
            this.moveTween = this.transform.DOMove(target, dur)
                .SetEase(easeType)
                .OnComplete(() => onComplete?.Invoke())
                .OnKill(() => movingCount--);
        }

        private void KillMoveTween()
        {
            if (this.moveTween == null) return;
            this.moveTween.Kill();
            this.moveTween = null;
        }

        private void KillAllTweens()
        {
            if (this.moveTween != null && this.moveTween.IsActive())
                Debug.LogWarning($"[CardMovement] {this.gameObject.name} KillAllTweens — killing active moveTween\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
            this.KillMoveTween();
            this.yTween?.Kill();
            this.yTween = null;
            this.rotateTween?.Kill();
            this.rotateTween = null;
            this.faceTween?.Kill();
            this.faceTween = null;
            this.rotateY180Tween?.Kill();
            this.rotateY180Tween = null;
            this.damageTween?.Kill();
            this.damageTween = null;
            this.attackTween?.Kill();
            this.attackTween = null;
        }
    }
}
