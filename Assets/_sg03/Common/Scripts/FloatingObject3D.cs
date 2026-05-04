using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Oscillates a 3D Transform up and down using a sine wave.
    /// </summary>
    [AddComponentMenu("SG03/Common/Floating Object 3D")]
    public class FloatingObject3D : SaiBehaviour
    {
        [Header("Float Settings")]
        [Tooltip("Peak displacement in world units.")]
        [SerializeField] private float amplitude = 0.2f;

        [Tooltip("Oscillations per second.")]
        [SerializeField] private float frequency = 0.8f;

        [Tooltip("Phase shift in seconds — stagger multiple objects.")]
        [SerializeField] private float phaseOffset = 0f;

        // ─── Runtime state ────────────────────────────────────────────────────────

        private Vector3 originLocalPosition;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()  => this.RecordOrigin();
        private void Update()    => this.ApplyFloat();
        private void OnDisable() => this.RestoreOrigin();

        // ─── Private methods ──────────────────────────────────────────────────────

        private void RecordOrigin()
        {
            this.originLocalPosition = this.transform.localPosition;
        }

        private void ApplyFloat()
        {
            this.transform.localPosition = this.ComputeFloatPosition();
        }

        private void RestoreOrigin()
        {
            this.transform.localPosition = this.originLocalPosition;
        }

        private Vector3 ComputeFloatPosition()
        {
            float phase   = (Time.time + this.phaseOffset) * this.frequency * Mathf.PI * 2f;
            float offsetY = Mathf.Sin(phase) * this.amplitude;
            return this.originLocalPosition + Vector3.up * offsetY;
        }
    }
}
