using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Continuously rotates a 3D Transform around a configurable local axis.
    /// </summary>
    [AddComponentMenu("SG03/Common/Rotating Object 3D")]
    public class RotatingObject3D : SaiBehaviour
    {
        [Header("Rotation Settings")]
        [Tooltip("Local axis to rotate around.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float rotationSpeed = 30f;

        // ─── Runtime state ────────────────────────────────────────────────────────

        private Quaternion originLocalRotation;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()  => this.RecordOrigin();
        private void Update()    => this.ApplyRotation();
        private void OnDisable() => this.RestoreOrigin();

        // ─── Private methods ──────────────────────────────────────────────────────

        private void RecordOrigin()
        {
            this.originLocalRotation = this.transform.localRotation;
        }

        private void ApplyRotation()
        {
            this.transform.Rotate(this.rotationAxis, this.rotationSpeed * Time.deltaTime, Space.Self);
        }

        private void RestoreOrigin()
        {
            this.transform.localRotation = this.originLocalRotation;
        }
    }
}
