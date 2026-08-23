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
        [SerializeField] private bool miniMode = true;

        [Header("Parenting")]
        [SerializeField] private Transform parent;
        [Tooltip("Local offset used for Alpha cards before it is cached in world space.")]
        [InspectorName("Alpha Parent Offset")]
        [SerializeField] private Vector3 alphaParentOffset = new Vector3(0f, -4.5f, 0.2f);
        [Tooltip("Local offset used for Omega cards before it is cached in world space.")]
        [InspectorName("Omega Parent Offset")]
        [SerializeField] private Vector3 omegaParentOffset = new Vector3(0f, -4.5f, -0.2f);
        [Tooltip("World-space height above the card. This is unaffected by card flips.")]
        [SerializeField, Min(0f)] private float aboveCardOffset = 0.2f;

        private VisualElement fill;
        private VisualElement root;
        private VisualElement track;
        private Label healthLabel;
        private Vector3 desiredWorldScale;
        private bool hasDesiredWorldScale;
        private Vector3 baseWorldRotation;
        private bool hasBaseWorldRotation;
        private Vector3 worldParentOffset;
        private bool hasWorldParentOffset;

        /// <summary>Runtime UI Toolkit element that renders the health fill.</summary>
        public VisualElement FillElement => this.fill;

        /// <summary>Runtime UI Toolkit root element of this health bar.</summary>
        public VisualElement RootElement => this.root;

        /// <summary>Runtime UI Toolkit element that renders the health track.</summary>
        public VisualElement TrackElement => this.track;

        private void LateUpdate()
        {
            this.UpdateWorldSpacePresentation();
        }

        private void OnEnable()
        {
            this.RefreshUiWhenEnabled();
        }

        protected override void Start()
        {
            this.InitializeHpBar();
        }

        private void InitializeHpBar()
        {
            this.RefreshUi();
        }

        private void RefreshUiWhenEnabled()
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
            this.UpdateWorldPositionFromParent();
            this.FaceMainCamera();
            this.CompensateParentScale();
        }

        private void FaceMainCamera()
        {
            if (!this.faceMainCamera || Camera.main == null) return;

            Vector3 directionFromCamera = this.transform.position - Camera.main.transform.position;
            if (directionFromCamera.sqrMagnitude < Mathf.Epsilon) return;

            float horizontalDistance = new Vector2(directionFromCamera.x, directionFromCamera.z).magnitude;
            Vector3 worldRotation = this.hasBaseWorldRotation
                ? this.baseWorldRotation
                : this.transform.eulerAngles;
            worldRotation.x = -Mathf.Atan2(directionFromCamera.y, horizontalDistance) * Mathf.Rad2Deg;
            this.transform.rotation = Quaternion.Euler(worldRotation);
        }

        public void SetHealth(float current, float maximum)
        {
            this.maxHealth = Mathf.Max(1f, maximum);
            this.currentHealth = Mathf.Clamp(current, 0f, this.maxHealth);
            this.RefreshUi();
        }

        /// <summary>Sets the health bar's world-space position.</summary>
        public void SetPosition(Vector3 worldPosition)
        {
            this.transform.position = worldPosition;
        }

        /// <summary>Makes this bar follow a parent while preserving its world position and scale.</summary>
        public void SetParent(Transform newParent, Owner owner)
        {
            if (newParent == null) return;

            this.desiredWorldScale = this.transform.lossyScale;
            this.hasDesiredWorldScale = true;
            this.parent = newParent;
            this.transform.SetParent(this.parent, true);
            this.UpdateParentOffset(owner);
            this.UpdateWorldPositionFromParent();
            this.baseWorldRotation = this.transform.eulerAngles;
            this.hasBaseWorldRotation = true;
            this.CompensateParentScale();
        }

        private Vector3 GetParentOffset(Owner owner)
        {
            return owner == Owner.omega ? this.omegaParentOffset : this.alphaParentOffset;
        }

        private void UpdateParentOffset(Owner owner)
        {
            this.transform.localPosition = this.GetParentOffset(owner);
            this.CacheWorldParentOffset();
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
            this.UpdateParentOffset(Owner.alpha);
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

        private void CacheWorldParentOffset()
        {
            if (this.parent == null) return;

            this.worldParentOffset = this.transform.position - this.parent.position;
            this.worldParentOffset.y = this.aboveCardOffset;
            this.hasWorldParentOffset = true;
        }

        private void UpdateWorldPositionFromParent()
        {
            if (!this.hasWorldParentOffset || this.parent == null || this.transform.parent != this.parent) return;

            Card3D card = this.parent.GetComponent<Card3D>();
            if (card != null && card.TryGetStatsCenterWorldPosition(out Vector3 statsCenter))
            {
                this.transform.position = statsCenter + Vector3.up * this.aboveCardOffset;
                return;
            }

            this.transform.position = this.parent.position + this.worldParentOffset;
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

        private void BindUi(bool forceRebind = false)
        {
            if (forceRebind) this.ClearUiElementReferences();

            if (this.uiDocument == null)
            {
                this.uiDocument = this.GetComponent<UIDocument>();
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

        private void ClearUiElementReferences()
        {
            this.fill = null;
            this.root = null;
            this.track = null;
            this.healthLabel = null;
        }

        private void LoadUiElement(ref VisualElement element, VisualElement uiRoot, string elementName)
        {
            if (element != null) return;
            element = uiRoot.Q<VisualElement>(elementName);
            if (element == null)
            {
                Debug.LogWarning($"{this.name}: UI element '{elementName}' is missing.", this.gameObject);
            }
        }

        private void LoadHealthLabel(VisualElement uiRoot)
        {
            if (this.healthLabel != null) return;
            this.healthLabel = uiRoot.Q<Label>("HealthLabel");
            if (this.healthLabel == null)
            {
                Debug.LogWarning($"{this.name}: UI element 'HealthLabel' is missing.", this.gameObject);
            }
        }

        public void RefreshUi()
        {
            this.BindUi(true);
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
