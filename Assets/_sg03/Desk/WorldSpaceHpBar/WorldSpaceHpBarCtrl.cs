using System.Collections;
using SG03.UI;
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
        [SerializeField] private BattleStateCtrl battleStateCtrl;

        [Header("Preview health")]
        [SerializeField, Min(0f)] private float currentHealth = 0f;
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
        private Owner parentOwner;

        private ClientActions subscribedClientActions;
        private Coroutine deferredUiRefreshRoutine;

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
            this.HandleEnabled();
        }

        private void OnDisable()
        {
            this.HandleDisabled();
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

        private void HandleEnabled()
        {
            this.RefreshUiWhenEnabled();
            this.RefreshUiWhenDocumentIsReady();
            this.BindClientActionEvents();
            this.RefreshMaxHealthFromBattleState();
        }

        private void HandleDisabled()
        {
            this.UnbindClientActionEvents();
            this.deferredUiRefreshRoutine = null;
        }

        private void RefreshUiWhenDocumentIsReady()
        {
            if (this.deferredUiRefreshRoutine != null) return;
            this.deferredUiRefreshRoutine = this.StartCoroutine(this.RefreshUiWhenDocumentIsReadyRoutine());
        }

        private IEnumerator RefreshUiWhenDocumentIsReadyRoutine()
        {
            yield return null;
            this.deferredUiRefreshRoutine = null;
            if (!this.isActiveAndEnabled) yield break;
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

        /// <summary>Immediately aligns this bar with its tracked card after a face-state change.</summary>
        public void RefreshWorldSpacePresentation()
        {
            this.UpdateWorldSpacePresentation();
        }

        private void FaceMainCamera()
        {
            if (!this.faceMainCamera || Camera.main == null) return;

            Vector3 directionFromCamera = this.transform.position - Camera.main.transform.position;
            if (directionFromCamera.sqrMagnitude < Mathf.Epsilon) return;

            // IMPORTANT: Update only pitch (X). Y/Z define the card-side orientation and must
            // remain unchanged; do not replace this with LookAt or transform.forward assignment.
            float horizontalDistance = new Vector2(directionFromCamera.x, directionFromCamera.z).magnitude;
            Vector3 worldRotation = this.hasBaseWorldRotation
                ? this.baseWorldRotation
                : this.transform.eulerAngles;
            worldRotation.x = -Mathf.Atan2(directionFromCamera.y, horizontalDistance) * Mathf.Rad2Deg;
            this.transform.rotation = Quaternion.Euler(worldRotation);
        }

        /// <summary>Gets the active DEF value for the card this bar is attached to.</summary>
        private int GetFinalDef()
        {
            return this.TryGetBattleCardSlot(out BattleCardSlot slot) ? slot.final_def : 0;
        }

        private int GetTotalDamageReceived()
        {
            return this.TryGetBattleCardSlot(out BattleCardSlot slot) ? slot.total_damage_received : 0;
        }

        private void RefreshMaxHealthFromBattleState()
        {
            if (!this.TryGetBattleCardSlot(out _)) return;

            this.SetMaxHealth(this.GetFinalDef());
        }

        private bool TryGetBattleCardSlot(out BattleCardSlot result)
        {
            result = null;
            // Pool activation occurs before Card3DCtrl assigns the tracked card through SetParent.
            // Use Unity's null check explicitly; ?. does not handle destroyed/unassigned Unity objects.
            if (this.parent == null) return false;

            Card3DCtrl card = this.parent.GetComponent<Card3DCtrl>();
            string inventoryItemId = card?.InventoryItemId;
            BattleState state = this.battleStateCtrl?.BattleState;
            if (state == null || string.IsNullOrEmpty(inventoryItemId)) return false;

            result = this.FindSlot(state.AlphaHand, inventoryItemId)
                ?? this.FindSlot(state.AlphaFrontLine, inventoryItemId)
                ?? this.FindSlot(state.AlphaBackLine, inventoryItemId)
                ?? this.FindSlot(state.AlphaTheVoid, inventoryItemId)
                ?? this.FindSlot(state.AlphaTheSource, inventoryItemId)
                ?? this.FindSlot(state.OmegaHand, inventoryItemId)
                ?? this.FindSlot(state.OmegaFrontLine, inventoryItemId)
                ?? this.FindSlot(state.OmegaBackLine, inventoryItemId)
                ?? this.FindSlot(state.OmegaTheVoid, inventoryItemId);
            return result != null;
        }

        private BattleCardSlot FindSlot(BattleCardSlot[] slots, string inventoryItemId)
        {
            if (slots == null) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot != null && slot.inventory_item_id == inventoryItemId) return slot;
            }

            return null;
        }

        public void SetHealth(float current, float maximum)
        {
            this.maxHealth = Mathf.Max(1f, maximum);
            this.currentHealth = Mathf.Clamp(current, 0f, this.maxHealth);
            this.RefreshUi();
        }

        private void SetMaxHealth(float maximum)
        {
            this.maxHealth = Mathf.Max(1f, maximum);
            this.currentHealth = Mathf.Clamp(this.currentHealth, 0f, this.maxHealth);
            this.RefreshUi();
        }

        private void ResetCurrentHealthForNewCard()
        {
            this.currentHealth = 0f;
        }

        /// <summary>Sets the health bar's world-space position.</summary>
        public void SetPosition(Vector3 worldPosition)
        {
            this.transform.position = worldPosition;
        }

        /// <summary>Assigns the card this bar follows without inheriting its transform.</summary>
        public void SetParent(Transform newParent, Owner owner)
        {
            if (newParent == null) return;

            bool isNewCard = this.parent != newParent;
            this.desiredWorldScale = this.transform.lossyScale;
            this.hasDesiredWorldScale = true;
            this.parent = newParent;
            this.parentOwner = owner;
            if (isNewCard) this.ResetCurrentHealthForNewCard();
            this.transform.SetParent(null, true);
            this.SetWorldParentOffset();
            this.UpdateWorldPositionFromParent();
            this.baseWorldRotation = this.transform.eulerAngles;
            this.hasBaseWorldRotation = true;
            this.BindClientActionEvents();
            this.RefreshMaxHealthFromBattleState();
        }

        private Vector3 GetParentOffset(Owner owner)
        {
            return owner == Owner.omega ? this.omegaParentOffset : this.alphaParentOffset;
        }

        private void SetWorldParentOffset()
        {
            this.worldParentOffset = this.GetParentOffset(this.parentOwner);
            this.worldParentOffset.y = this.aboveCardOffset;
            this.hasWorldParentOffset = true;
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

        /// <summary>Reapplies the world-space follow offset for the assigned parent.</summary>
        public void UpdateParent()
        {
            if (this.parent == null)
            {
                Debug.LogWarning($"{this.name}: Parent is not assigned.", this);
                return;
            }

            this.desiredWorldScale = this.transform.lossyScale;
            this.hasDesiredWorldScale = true;
            this.transform.SetParent(null, true);
            this.SetWorldParentOffset();
            this.UpdateWorldPositionFromParent();
        }

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.CacheDesiredWorldScale();
            this.BindUi();
            this.LoadBattleStateCtrl();
        }

        private void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = FindAnyObjectByType<BattleStateCtrl>();
        }

        private void BindClientActionEvents()
        {
            this.LoadBattleStateCtrl();
            ClientActions clientActions = this.battleStateCtrl?.ClientActions;
            if (clientActions == this.subscribedClientActions) return;

            this.UnbindClientActionEvents();
            this.subscribedClientActions = clientActions;
            if (this.subscribedClientActions != null)
            {
                this.subscribedClientActions.OnCardTakeDamageExecuted += this.OnCardTakeDamageExecuted;
            }
        }

        private void UnbindClientActionEvents()
        {
            if (this.subscribedClientActions == null) return;
            this.subscribedClientActions.OnCardTakeDamageExecuted -= this.OnCardTakeDamageExecuted;
            this.subscribedClientActions = null;
        }

        private void OnCardTakeDamageExecuted(string targetCardId)
        {
            if (this.parent == null) return;

            string cardId = this.parent.GetComponent<Card3DCtrl>()?.InventoryItemId;
            if (string.IsNullOrEmpty(cardId) || cardId != targetCardId) return;
            this.RefreshHealthFromDamageAction();
        }

        private void RefreshHealthFromDamageAction()
        {
            // The client action selects when to refresh; the battle state is the source of truth.
            this.SetHealth(this.GetTotalDamageReceived(), this.GetFinalDef());
            // The damage animation starts in the same action. Re-anchor now rather than waiting
            // for the next LateUpdate, so the bar remains above the card for this frame as well.
            this.RefreshWorldSpacePresentation();
        }

        /// <summary>Refreshes both values from battle state after an end-turn action resets card state.</summary>
        public void RefreshHealthFromTurnEnd()
        {
            if (!this.TryGetBattleCardSlot(out _)) return;
            this.SetHealth(this.GetTotalDamageReceived(), this.GetFinalDef());
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

        private void UpdateWorldPositionFromParent()
        {
            if (!this.hasWorldParentOffset || this.parent == null) return;

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
                // Expected while SpawnInactive configures this object before it is enabled.
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
