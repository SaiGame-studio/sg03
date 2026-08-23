using UnityEngine;
using UnityEngine.UIElements;

namespace SG03
{
    /// <summary>Drives a UI Toolkit health bar displayed above a world-space target.</summary>
    [AddComponentMenu("SG03/BattleState/World Space HP Bar")]
    public sealed class WorldSpaceHpBarCtrl : PoolObj
    {
        [Header("Required runtime references")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Preview health")]
        [SerializeField, Min(0f)] private float currentHealth = 65f;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool faceMainCamera = true;

        [Header("Bar appearance")]
        [SerializeField, Min(1f)] private float barWidth = 350;
        [SerializeField, Min(1f)] private float barHeight = 20f;
        [SerializeField, Min(0f)] private float borderThickness = 4f;
        [Tooltip("Green is on the left and red is on the right. The bar reads this gradient from left to right as HP increases.")]
        [SerializeField] private Gradient healthColorGradient = CreateDefaultHealthColorGradient();
        [Tooltip("Offset of the HP values from the top-left corner of HealthBarRoot.")]
        [SerializeField] private Vector2 healthLabelOffset = new(0, -45f);
        [SerializeField, Min(1f)] private float healthLabelFontSize = 60f;

        [Header("Display mode")]
        [Tooltip("When enabled, only the HP bar is displayed. Disable it to also show the current and maximum HP.")]
        [SerializeField] private bool miniMode;

        [Header("Parenting")]
        [SerializeField] private Transform parent;
        [SerializeField] private Vector3 parentOffset = new Vector3(0f, 1.5f, 0f);

        private VisualElement fill;
        private VisualElement root;
        private VisualElement track;
        private Label healthLabel;
        private Vector3 desiredWorldScale;
        private bool hasDesiredWorldScale;

        private void LateUpdate()
        {
            this.UpdateWorldSpacePresentation();
        }

        protected override void Start()
        {
            this.RefreshUi();

        }

        /// <summary>Returns the pool key used by <see cref="Spawner{T}"/>.</summary>
        public override string GetName()
        {
            return nameof(WorldSpaceHpBarCtrl);
        }

        private void UpdateWorldSpacePresentation()
        {
            this.FaceMainCamera();
            this.CompensateParentScale();
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

        /// <summary>Sets whether this bar displays only its fill or also its HP values.</summary>
        public void SetMiniMode(bool enabled)
        {
            this.miniMode = enabled;
            this.RefreshHealthLabel();
        }

        /// <summary>Switches between the mini bar-only view and the full view with HP values.</summary>
        public void ToggleDisplayMode()
        {
            this.SetMiniMode(!this.miniMode);
        }

        /// <summary>Makes this HP bar a child of Parent and applies Parent Offset in local space.</summary>
        public void UpdateParent()
        {
            if (this.parent == null)
            {
                Debug.LogWarning($"{this.name}: Parent is not assigned.", this);
                return;
            }

            this.desiredWorldScale = this.transform.lossyScale;
            this.hasDesiredWorldScale = true;
            this.transform.SetParent(this.parent, false);
            this.transform.localPosition = this.parentOffset;
            this.CompensateParentScale();
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.CacheDesiredWorldScale();
            this.BindUi();
        }

        // This UI is returned explicitly through ObjectPool, so it needs no Despawn component.
        protected override void LoadDespawn()
        {
        }

        private void CacheDesiredWorldScale()
        {
            if (this.hasDesiredWorldScale) return;
            this.desiredWorldScale = this.transform.lossyScale;
            this.hasDesiredWorldScale = true;
        }

        private void CompensateParentScale()
        {
            if (this.parent == null || this.transform.parent != this.parent) return;
            this.CacheDesiredWorldScale();

            Vector3 parentScale = this.parent.lossyScale;
            if (Mathf.Approximately(parentScale.x, 0f)
                || Mathf.Approximately(parentScale.y, 0f)
                || Mathf.Approximately(parentScale.z, 0f)) return;

            this.transform.localScale = new Vector3(
                this.desiredWorldScale.x / parentScale.x,
                this.desiredWorldScale.y / parentScale.y,
                this.desiredWorldScale.z / parentScale.z);
        }

        private void BindUi()
        {
            if (this.uiDocument == null)
            {
                this.uiDocument = this.GetComponent<UIDocument>();
                Debug.LogWarning($"{this.name}: Load UIDocument", this.gameObject);
            }

            if (this.uiDocument == null)
            {
                Debug.LogWarning($"{this.name}: UIDocument is missing.", this.gameObject);
                return;
            }

            VisualElement uiRoot = this.uiDocument.rootVisualElement;
            if (uiRoot == null)
            {
                Debug.LogWarning($"{this.name}: UIDocument root is missing.", this.gameObject);
                return;
            }

            this.LoadUiElement(ref this.root, uiRoot, "HealthBarRoot");
            this.LoadUiElement(ref this.track, uiRoot, "HealthTrack");
            this.LoadUiElement(ref this.fill, uiRoot, "HealthFill");
            this.LoadHealthLabel(uiRoot);
        }

        private void LoadUiElement(ref VisualElement element, VisualElement uiRoot, string elementName)
        {
            if (element != null) return;
            element = uiRoot.Q<VisualElement>(elementName);
            Debug.LogWarning($"{this.name}: Load {elementName}", this.gameObject);
        }

        private void LoadHealthLabel(VisualElement uiRoot)
        {
            if (this.healthLabel != null) return;
            this.healthLabel = uiRoot.Q<Label>("HealthLabel");
            Debug.LogWarning($"{this.name}: Load HealthLabel", this.gameObject);
        }

        public void RefreshUi()
        {
            if (this.fill == null || this.healthLabel == null) this.BindUi();
            if (this.fill == null) return;
            float ratio = Mathf.Clamp01(this.currentHealth / this.maxHealth);
            this.ApplyBarAppearance(ratio);
            this.fill.style.width = Length.Percent(ratio * 100f);

            this.RefreshHealthLabel();
        }

        private void RefreshHealthLabel()
        {
            if (this.healthLabel == null) this.BindUi();
            if (this.healthLabel == null) return;

            // Keep the label in the UI layout when hidden so the HP bar never changes position.
            this.healthLabel.style.visibility = this.miniMode ? Visibility.Hidden : Visibility.Visible;
            this.healthLabel.text = $"{Mathf.CeilToInt(this.currentHealth)} / {Mathf.CeilToInt(this.maxHealth)}";
        }

        private void ApplyBarAppearance(float healthRatio)
        {
            this.fill.style.backgroundColor = this.healthColorGradient.Evaluate(healthRatio);

            if (this.root != null)
            {
                this.root.style.width = this.barWidth;
                this.root.style.height = this.barHeight;
            }

            if (this.track != null)
            {
                this.track.style.width = this.barWidth;
                this.track.style.height = this.barHeight;
                this.track.style.borderTopWidth = this.borderThickness;
                this.track.style.borderRightWidth = this.borderThickness;
                this.track.style.borderBottomWidth = this.borderThickness;
                this.track.style.borderLeftWidth = this.borderThickness;
            }

            if (this.healthLabel != null)
            {
                this.healthLabel.style.width = this.barWidth;
                this.healthLabel.style.height = this.barHeight;
                this.healthLabel.style.left = this.healthLabelOffset.x;
                this.healthLabel.style.top = this.healthLabelOffset.y;
                this.healthLabel.style.fontSize = this.healthLabelFontSize;
            }
        }

        private static Gradient CreateDefaultHealthColorGradient()
        {
            return new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(new Color(0.18f, 0.8f, 0.28f), 0f),
                    new GradientColorKey(new Color(0.95f, 0.72f, 0.12f), 0.5f),
                    new GradientColorKey(new Color(0.9f, 0.1f, 0.12f), 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                },
            };
        }
    }
}
