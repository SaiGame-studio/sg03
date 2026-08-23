using SaiGame.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03
{
    /// <summary>Drives a UI Toolkit health bar displayed above a world-space target.</summary>
    [AddComponentMenu("SG03/BattleState/World Space HP Bar")]
    public sealed class WorldSpaceHpBarCtrl : SaiBehaviour
    {
        [Header("Required runtime references")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Preview health")]
        [SerializeField, Min(0f)] private float currentHealth = 65f;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool faceMainCamera = true;

        [Header("Parenting")]
        [SerializeField] private Transform parent;
        [SerializeField] private Vector3 parentOffset = new Vector3(0f, 1.5f, 0f);

        private VisualElement fill;
        private Label healthLabel;

        private void LateUpdate()
        {
            this.FaceMainCamera();
        }

        private void FaceMainCamera()
        {
            if (!this.faceMainCamera || Camera.main == null) return;
            Vector3 direction = this.transform.position - Camera.main.transform.position;
            Quaternion cameraFacingRotation = Quaternion.LookRotation(direction);
            Vector3 rotation = cameraFacingRotation.eulerAngles;
            rotation.y = this.transform.eulerAngles.y;
            this.transform.eulerAngles = rotation;
        }

        public void SetHealth(float current, float maximum)
        {
            this.maxHealth = Mathf.Max(1f, maximum);
            this.currentHealth = Mathf.Clamp(current, 0f, this.maxHealth);
            this.RefreshUi();
        }

        /// <summary>Makes this HP bar a child of Parent and applies Parent Offset in local space.</summary>
        public void UpdateParent()
        {
            if (this.parent == null)
            {
                Debug.LogWarning($"{this.name}: Parent is not assigned.", this);
                return;
            }

            this.transform.SetParent(this.parent, false);
            this.transform.localPosition = this.parentOffset;
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.BindUi();
        }

        private void BindUi()
        {
            if (this.uiDocument == null) this.uiDocument = this.GetComponent<UIDocument>();
            if (this.uiDocument == null) return;
            VisualElement root = this.uiDocument.rootVisualElement;
            this.fill = root.Q<VisualElement>("HealthFill");
            this.healthLabel = root.Q<Label>("HealthLabel");
        }

        public void RefreshUi()
        {
            if (this.fill == null || this.healthLabel == null) this.LoadComponents();
            if (this.fill == null || this.healthLabel == null) return;
            float ratio = Mathf.Clamp01(this.currentHealth / this.maxHealth);
            this.fill.style.width = Length.Percent(ratio * 100f);
            this.healthLabel.text = $"HP {Mathf.CeilToInt(this.currentHealth)} / {Mathf.CeilToInt(this.maxHealth)}";
        }
    }
}
