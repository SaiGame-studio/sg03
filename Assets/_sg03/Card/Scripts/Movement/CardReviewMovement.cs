using DG.Tweening;
using System;
using SaiGame.Services;
using SG03.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    /// <summary>
    /// Temporary review utility that lets a card fly up by a configurable distance
    /// or fly back down to the position it occupied when the scene started.
    /// Uses DOTween for smooth movement.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Review Movement")]
    public class CardReviewMovement : SaiBehaviour
    {
        [Header("Show")]
        [Tooltip("Distance (world units) to move upward along the Y axis.")]
        [SerializeField] private float flyUpDistance = 21f;

        [Header("Hide")]
        [Tooltip("World-space position recorded at Start. Hide returns here.")]
        [SerializeField] private Vector3 originPosition;

        private bool isShown;

        /// <summary>Returns true if the card is currently in the shown (fly-up) state.</summary>
        public bool IsShown => isShown;

        /// <summary>
        /// Raised when middle-click requests that the preview close. A handler
        /// returns true when it owns the close flow; otherwise this component
        /// returns the card itself.
        /// </summary>
        public event Func<bool> PreviewCancelRequested;

        [Header("Animation")]
        [Tooltip("Duration of the fly animation in seconds.")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("Ease curve applied to both Fly Up and Fly Down.")]
        [SerializeField] private Ease ease = Ease.OutQuad;

        [Header("Rotation")]
        [Tooltip("Degrees per pixel of horizontal mouse drag while the card is shown.")]
        [SerializeField] private float rotateSpeed = 0.5f;

        [Header("Zoom")]
        [Tooltip("Camera used to calculate near/far movement for the reviewed card.")]
        [SerializeField] private Camera reviewCamera;

        [Tooltip("World-space distance moved per mouse-wheel step.")]
        [SerializeField] private float zoomStep = 1f;

        [Tooltip("Closest distance the reviewed card may reach from the camera.")]
        [SerializeField] private float minCameraDistance = 8f;

        [Tooltip("Farthest distance the reviewed card may reach from the camera.")]
        [SerializeField] private float maxCameraDistance = 22f;

        [Header("Pan")]
        [Tooltip("World-space distance moved per pixel while dragging with the right mouse button.")]
        [SerializeField] private float panSpeed = 0.01f;

        private Vector2 lastMousePos;
        private Vector2 lastPanMousePos;
        private bool isShowAnimationPlaying;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadReviewCamera();
        }

        private void LoadReviewCamera()
        {
            if (this.reviewCamera != null) return;
            this.reviewCamera = Camera.main;
            Debug.LogWarning(transform.name + ": LoadReviewCamera", gameObject);
        }

        protected override void Start()
        {
            base.Start();
            this.InitializeReviewState();
        }

        private void InitializeReviewState()
        {
            this.originPosition = transform.position;
            this.isShown = false;
            this.isShowAnimationPlaying = false;
        }

        private void Update()
        {
            this.HandleFrameInput();
        }

        private void HandleFrameInput()
        {
            if (ModalDimLayer.IsInputBlocked) return;
            this.HandlePreviewCancel();
            this.HandleRotation();
            this.HandleZoom();
            this.HandlePan();
        }

        private void OnDestroy() => this.KillMovementTweens();

        private void KillMovementTweens() => this.transform.DOKill();

        private void HandlePreviewCancel()
        {
            if (!this.isShown || this.isShowAnimationPlaying) return;
            if (Mouse.current?.middleButton.wasPressedThisFrame != true) return;

            if (this.TryHandlePreviewCancel()) return;
            this.Hide();
        }

        private bool TryHandlePreviewCancel()
        {
            if (this.PreviewCancelRequested == null) return false;

            foreach (Delegate callback in this.PreviewCancelRequested.GetInvocationList())
            {
                if (((Func<bool>)callback).Invoke()) return true;
            }

            return false;
        }

        // ─── Rotation ─────────────────────────────────────────────────────────────

        private void HandleRotation()
        {
            if (!this.isShown) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                this.lastMousePos = mouse.position.ReadValue();
                return;
            }

            if (!mouse.leftButton.isPressed) return;

            Vector2 current   = mouse.position.ReadValue();
            float deltaX      = current.x - this.lastMousePos.x;
            this.lastMousePos = current;
            this.transform.Rotate(Vector3.up, -deltaX * this.rotateSpeed, Space.World);
        }

        private void HandleZoom()
        {
            if (!this.isShown || this.isShowAnimationPlaying) return;
            if (this.reviewCamera == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scrollY, 0f)) return;

            Vector3 cameraPosition = this.reviewCamera.transform.position;
            Vector3 cameraToCard = this.transform.position - cameraPosition;
            float currentDistance = cameraToCard.magnitude;
            if (currentDistance <= Mathf.Epsilon) return;

            float minimumDistance = Mathf.Max(0.1f, this.minCameraDistance);
            float maximumDistance = Mathf.Max(minimumDistance, this.maxCameraDistance);
            float scrollStep = Mathf.Clamp(scrollY, -1f, 1f) * this.zoomStep;
            float targetDistance = Mathf.Clamp(
                currentDistance - scrollStep,
                minimumDistance,
                maximumDistance);

            this.transform.position = cameraPosition + cameraToCard.normalized * targetDistance;
        }

        private void HandlePan()
        {
            if (!this.isShown || this.isShowAnimationPlaying) return;
            if (this.reviewCamera == null) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.rightButton.wasPressedThisFrame)
            {
                this.lastPanMousePos = mouse.position.ReadValue();
                return;
            }

            if (!mouse.rightButton.isPressed) return;

            Vector2 currentMousePosition = mouse.position.ReadValue();
            Vector2 pointerDelta = currentMousePosition - this.lastPanMousePos;
            this.lastPanMousePos = currentMousePosition;

            Transform cameraTransform = this.reviewCamera.transform;
            Vector3 panOffset =
                cameraTransform.right * (pointerDelta.x * this.panSpeed) +
                cameraTransform.up * (pointerDelta.y * this.panSpeed);
            this.transform.position += panOffset;
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Smoothly moves the card upward by <see cref="flyUpDistance"/> on the Y axis
        /// while spinning 360° on the Y axis. Ends face-up (Y rotation = 0°).
        /// Does nothing if the card is already shown.
        /// </summary>
        public void Show()
        {
            if (isShown) return;
            isShown = true;
            isShowAnimationPlaying = true;

            Vector3 moveTarget   = transform.position + new Vector3(0f, flyUpDistance, 0f);
            Vector3 rotateTarget = transform.eulerAngles + new Vector3(0f, 360f, 0f);

            transform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(moveTarget, duration).SetEase(ease));
            seq.Join(transform.DORotate(rotateTarget, duration, RotateMode.FastBeyond360).SetEase(ease));
            seq.OnComplete(() => this.isShowAnimationPlaying = false);
        }

        /// <summary>
        /// Smoothly returns the card to its origin position while spinning
        /// 360° on the Y axis. Ends face-up (Y rotation = 0°).
        /// Resets the shown state so <see cref="Show"/> can be called again.
        /// Returns the Sequence so callers can chain an OnComplete callback.
        /// </summary>
        public Sequence Hide()
        {
            isShown = false;
            isShowAnimationPlaying = false;

            Vector3 rotateTarget = transform.eulerAngles + new Vector3(0f, 360f, 0f);

            transform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOMove(originPosition, duration).SetEase(ease));
            seq.Join(transform.DORotate(rotateTarget, duration, RotateMode.FastBeyond360).SetEase(ease));
            seq.AppendCallback(() => transform.eulerAngles = new Vector3(transform.eulerAngles.x, 0f, transform.eulerAngles.z));
            return seq;
        }
    }
}
