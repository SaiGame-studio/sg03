using SaiGame.Services;
using SG03.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03
{
    /// <summary>
    /// World-space UI Toolkit tooltip that follows the hovered card in 3D.
    /// Attach to a GameObject that also has a UIDocument component.
    /// The UIDocument's PanelSettings must use renderMode = WorldSpace.
    /// </summary>
    [AddComponentMenu("SG03/BattleState/Card Hover Tooltip 3D Ctrl")]
    public class CardHoverTooltip3DCtrl : SaiBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument      uiDocument;
        [SerializeField] private BattleStateCtrl battleStateCtrl;

        [Header("3D Positioning")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.8f, 0f);

        private Label      finalDefLabel;
        private Label      damageLabel;
        private Card3DCtrl hoveredCard;

        // ─── LoadComponents ───────────────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadUIDocument();
            this.LoadBattleStateCtrl();
        }

        private void LoadUIDocument()
        {
            if (this.uiDocument != null) return;
            this.uiDocument = this.GetComponent<UIDocument>();
            Debug.LogWarning(this.transform.name + ": LoadUIDocument", this.gameObject);
        }

        private void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = this.GetComponentInParent<BattleStateCtrl>(true);
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        protected override void Start()
        {
            base.Start();
            this.BindLabels();
            this.Hide();
        }

        private void OnEnable()  => this.Subscribe();
        private void OnDisable() => this.Unsubscribe();
        private void Update()    => this.TickTooltipTransform();

        // ─── Subscription ─────────────────────────────────────────────────────────

        private void Subscribe()
        {
            Card3DCtrl.HoverEntered += this.OnHoverEntered;
            Card3DCtrl.HoverExited  += this.OnHoverExited;
        }

        private void Unsubscribe()
        {
            Card3DCtrl.HoverEntered -= this.OnHoverEntered;
            Card3DCtrl.HoverExited  -= this.OnHoverExited;
        }

        // ─── Hover events ─────────────────────────────────────────────────────────

        private void OnHoverEntered(Card3DCtrl card)
        {
            if (!card.IsCharacter()) return;
            if (card.Location != Location.in_front) return;
            this.hoveredCard = card;
            this.RefreshData();
            this.Show();
        }

        private void OnHoverExited(Card3DCtrl card)
        {
            // Intentionally empty: tooltip stays visible and holds its last position
            // until the player hovers over a new card.
        }

        // ─── Transform tick ───────────────────────────────────────────────────────

        private void TickTooltipTransform()
        {
            this.FollowHoveredCard();
            this.FaceCamera();
        }

        private void FollowHoveredCard()
        {
            if (this.hoveredCard == null) return;
            this.transform.position = this.hoveredCard.transform.position + this.offset;
        }

        private void FaceCamera()
        {
            if (Camera.main == null) return;
            this.transform.forward = this.transform.position - Camera.main.transform.position;
        }

        // ─── Label binding ────────────────────────────────────────────────────────

        private void BindLabels()
        {
            if (this.uiDocument?.rootVisualElement == null) return;
            VisualElement root    = this.uiDocument.rootVisualElement;
            this.finalDefLabel    = root.Q<Label>("FinalDefLabel");
            this.damageLabel      = root.Q<Label>("DamageReceivedLabel");
        }

        // ─── Data refresh ─────────────────────────────────────────────────────────

        private void RefreshData()
        {
            BattleCardSlot slot = this.FindSlot();
            if (slot == null) return;
            this.SetFinalDef(slot.final_def);
            this.SetDamageReceived(slot.total_damage_received);
        }

        private void SetFinalDef(int value)
        {
            if (this.finalDefLabel == null) return;
            this.finalDefLabel.text = $"DEF: {value}";
        }

        private void SetDamageReceived(int value)
        {
            if (this.damageLabel == null) return;
            this.damageLabel.text = $"DMG: {value}";
        }

        // ─── Visibility ───────────────────────────────────────────────────────────

        private void Show()
        {
            if (this.uiDocument?.rootVisualElement == null) return;
            this.uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            if (this.uiDocument?.rootVisualElement == null) return;
            this.uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        // ─── Slot lookup ──────────────────────────────────────────────────────────

        private BattleCardSlot FindSlot()
        {
            if (this.hoveredCard == null) return null;
            BattleState state = this.battleStateCtrl?.BattleState;
            if (state == null) return null;
            return this.FindSlotInState(state, this.hoveredCard.InventoryItemId);
        }

        private BattleCardSlot FindSlotInState(BattleState state, string inventoryItemId)
        {
            return this.FindInArray(state.AlphaHand,      inventoryItemId)
                ?? this.FindInArray(state.AlphaFrontLine, inventoryItemId)
                ?? this.FindInArray(state.AlphaBackLine,  inventoryItemId)
                ?? this.FindInArray(state.AlphaTheVoid,   inventoryItemId)
                ?? this.FindInArray(state.AlphaTheSource, inventoryItemId)
                ?? this.FindInArray(state.OmegaHand,      inventoryItemId)
                ?? this.FindInArray(state.OmegaFrontLine, inventoryItemId)
                ?? this.FindInArray(state.OmegaBackLine,  inventoryItemId)
                ?? this.FindInArray(state.OmegaTheVoid,   inventoryItemId);
        }

        private BattleCardSlot FindInArray(BattleCardSlot[] slots, string inventoryItemId)
        {
            if (slots == null) return null;
            if (string.IsNullOrEmpty(inventoryItemId)) return null;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot == null) continue;
                if (slot.inventory_item_id == inventoryItemId) return slot;
            }
            return null;
        }
    }
}
