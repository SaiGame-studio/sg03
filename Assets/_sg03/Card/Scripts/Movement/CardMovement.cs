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

        [Tooltip("World-space axis used when FaceState is Unknown (first-time flip).")]
        [SerializeField] private Vector3 flipAxisUnknown = new Vector3(1f, 0f, 0f);

        [Tooltip("World-space axis used when flipping between FaceUp and FaceDown.")]
        [SerializeField] private Vector3 flipAxisUpDown = new Vector3(0f, 0f, 1f);

        [Tooltip("World units the card rises during the flip.")]
        [SerializeField] private float flipRiseHeight = 5f;

        [Tooltip("Duration of each flip phase in seconds.")]
        [SerializeField] private float flipDuration = 0.4f;

        [Tooltip("Ease curve for the flip.")]
        [SerializeField] private Ease flipEase = Ease.InOutQuad;

        // ─── Runtime state ────────────────────────────────────────────────────────

        [Header("State")]
        [SerializeField] private Location location;
        [SerializeField] private FaceState faceState = FaceState.Unknown;

        private float handAnchorY;
        private bool  isSelected;
        private bool  isFlipping;
        private Tween yTween;
        private Tween moveTween;
        private Sequence faceTween;
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

        public Location Location   => this.location;
        public bool    IsFlipping  => this.isFlipping;

        /// <summary>
        /// Smoothly moves the card to the specified world-space <paramref name="target"/> position.
        /// Any in-progress tween is cancelled before starting the new one.
        /// </summary>
        public void MoveAndRotate(Transform target, Location destination)
        {
            if (this.isFlipping) return;
            this.location = destination;
            this.RecordHandAnchor(target, destination);
            this.KillAllTweens();
            this.StartMoveTween(target.position, this.duration, this.ease);
            this.transform.DORotateQuaternion(target.rotation, this.duration).SetEase(this.ease);
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
            this.StartMoveTween(target.position, this.duration, this.ease);
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
            this.DoFaceFlipNoRise(this.faceUpRotation, this.flipAxisUnknown);
        }

        /// <summary>Rotates the card to face-down using the Unknown axis, without rising.</summary>
        public void FaceDownUnknown()
        {
            if (this.isFlipping) return;
            this.faceState = FaceState.FaceDown;
            this.DoFaceFlipNoRise(this.faceDownRotation, this.flipAxisUnknown);
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

        private void DoFaceFlipNoRise(Vector3 targetEulers, Vector3 axis)
        {
            float totalTime = this.flipDuration * 2f;
            float angle     = this.ComputeFlipAngle(targetEulers, axis);

            this.isFlipping = true;
            this.faceTween?.Kill();
            this.faceTween = DOTween.Sequence();
            this.faceTween.Insert(0f,
                this.transform.DORotate(axis.normalized * angle, totalTime, RotateMode.WorldAxisAdd)
                    .SetEase(this.flipEase));
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
            this.StartMoveTween(point.position, this.fullDetailDuration, this.fullDetailEase);
            this.transform.DORotateQuaternion(point.rotation, this.fullDetailDuration).SetEase(this.fullDetailEase);
        }

        /// <summary>
        /// Returns the card from full-detail back to the position it occupied just before entering full detail.
        /// </summary>
        public void ReturnFromFullDetail()
        {
            if (this.isFlipping) return;
            this.KillAllTweens();
            this.StartMoveTween(this.preFullDetailPosition, this.fullDetailReturnDuration, this.fullDetailReturnEase);
            this.transform.DORotateQuaternion(this.preFullDetailRotation, this.fullDetailReturnDuration).SetEase(this.fullDetailReturnEase);
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

        private void StartMoveTween(Vector3 target, float dur, Ease easeType)
        {
            movingCount++;
            this.moveTween = this.transform.DOMove(target, dur)
                .SetEase(easeType)
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
            this.KillMoveTween();
            this.yTween?.Kill();
            this.yTween = null;
            this.faceTween?.Kill();
            this.faceTween = null;
            this.transform.DOKill();
        }
    }
}
