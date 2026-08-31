using SaiGame.Services;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SG03
{
    /// <summary>
    /// Lets the single card currently displayed in full-detail mode be inspected
    /// with mouse-wheel zoom and right-button pan, matching the lobby card
    /// preview.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card Full Detail Manipulator")]
    public class CardFullDetailManipulator : SaiBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera reviewCamera;

        [Header("Zoom")]
        [SerializeField] private float zoomStep = 1f;
        [SerializeField] private float minCameraDistance = 8f;
        [SerializeField] private float maxCameraDistance = 22f;

        [Header("Pan")]
        [SerializeField] private float panSpeed = 0.01f;

        private bool isInteractionActive;
        private Vector2 lastPanMousePosition;
        private Vector3 fullDetailPosition;
        private bool hasFullDetailPosition;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadReviewCamera();
        }

        private void LoadReviewCamera()
        {
            if (this.reviewCamera != null) return;
            this.reviewCamera = Camera.main;
            Debug.LogWarning(this.transform.name + ": LoadReviewCamera", this.gameObject);
        }

        private void Update() => this.HandleInput();

        private void HandleInput()
        {
            if (!this.isInteractionActive || CardMovement.IsAnyCardMoving) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || this.reviewCamera == null) return;

            this.HandleZoom(mouse);
            this.HandlePan(mouse);
        }

        private void HandleZoom(Mouse mouse)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scrollY, 0f)) return;

            this.CaptureFullDetailPosition();

            Vector3 cameraPosition = this.reviewCamera.transform.position;
            Vector3 cameraToCard = this.transform.position - cameraPosition;
            float currentDistance = cameraToCard.magnitude;
            if (currentDistance <= Mathf.Epsilon) return;

            float minimumDistance = Mathf.Max(0.1f, this.minCameraDistance);
            float maximumDistance = Mathf.Max(minimumDistance, this.maxCameraDistance);
            float scrollStep = Mathf.Clamp(scrollY, -1f, 1f) * this.zoomStep;
            float targetDistance = Mathf.Clamp(currentDistance - scrollStep, minimumDistance, maximumDistance);
            this.transform.position = cameraPosition + cameraToCard.normalized * targetDistance;
        }

        private void HandlePan(Mouse mouse)
        {
            if (mouse.rightButton.wasPressedThisFrame)
            {
                this.CaptureFullDetailPosition();
                this.lastPanMousePosition = mouse.position.ReadValue();
                return;
            }

            if (!mouse.rightButton.isPressed) return;

            Vector2 mousePosition = mouse.position.ReadValue();
            Vector2 pointerDelta = mousePosition - this.lastPanMousePosition;
            this.lastPanMousePosition = mousePosition;

            Transform cameraTransform = this.reviewCamera.transform;
            this.transform.position +=
                cameraTransform.right * (pointerDelta.x * this.panSpeed) +
                cameraTransform.up * (pointerDelta.y * this.panSpeed);
        }

        public void SetInteractionActive(bool value)
        {
            if (!value) this.ResetFullDetailPosition();
            this.isInteractionActive = value;
            if (value) this.hasFullDetailPosition = false;
        }

        private void CaptureFullDetailPosition()
        {
            if (this.hasFullDetailPosition) return;
            this.fullDetailPosition = this.transform.position;
            this.hasFullDetailPosition = true;
        }

        private void ResetFullDetailPosition()
        {
            if (!this.hasFullDetailPosition) return;
            this.transform.position = this.fullDetailPosition;
            this.hasFullDetailPosition = false;
        }
    }
}
