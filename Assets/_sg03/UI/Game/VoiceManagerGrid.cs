using System.Collections.Generic;
using SaiGame.Services;
using SG03;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SG03.UI
{
    public class VoiceManagerGrid : SaiBehaviour
    {
        [Header("Grid Size")]
        [SerializeField, Min(1)] private int columns = 4;
        [SerializeField, Min(1)] private int rows = 2;

        [Header("Card Size (9:16 Default)")]
        [SerializeField, Min(1)] private int cardWidth = 90;
        [SerializeField, Min(1)] private int cardHeight = 160;

        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private BattleStateCtrl battleStateCtrl;

        private VisualElement overlay;
        private VisualElement content;
        private VisualElement panel;
        private Label alphaVoidCountLabel;
        private Label titleLabel;
        private Label pageLabel;
        private Button closeButton;
        private Button previousButton;
        private Button nextButton;
        private Card3DCtrl hoveredCard;
        private BattleState subscribedBattleState;
        private int currentPage;
        private bool isVisible;

        public int Columns => Mathf.Max(1, this.columns);
        public int Rows => Mathf.Max(1, this.rows);
        public int CardWidth => Mathf.Max(1, this.cardWidth);
        public int CardHeight => Mathf.Max(1, this.cardHeight);

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
            if (this.uiDocument == null) return;
            Debug.LogWarning(this.transform.name + ": LoadUIDocument", this.gameObject);
        }

        private void LoadBattleStateCtrl()
        {
            if (this.battleStateCtrl != null) return;
            this.battleStateCtrl = FindFirstObjectByType<BattleStateCtrl>();
            if (this.battleStateCtrl == null) return;
            Debug.LogWarning(this.transform.name + ": LoadBattleStateCtrl", this.gameObject);
        }

        protected override void Start()
        {
            base.Start();
            this.InitializeGrid();
        }

        private void InitializeGrid()
        {
            if (this.uiDocument == null)
            {
                Debug.LogError(this.transform.name + ": VoiceManagerGrid requires a UIDocument reference.", this.gameObject);
                return;
            }

            this.BindElements(this.uiDocument.rootVisualElement);
            this.RegisterCallbacks();
            this.SubscribeToCardHoverEvents();
            this.SubscribeToBattleState();
            this.Hide();
        }

        public void SetGridSize(int columnCount, int rowCount)
        {
            this.columns = Mathf.Max(1, columnCount);
            this.rows = Mathf.Max(1, rowCount);
            this.currentPage = 0;
            if (this.isVisible) this.Refresh();
        }

        public void SetCardSize(int width, int height)
        {
            this.cardWidth = Mathf.Max(1, width);
            this.cardHeight = Mathf.Max(1, height);
            if (this.isVisible) this.Refresh();
        }

        private void Update()
        {
            this.TickGrid();
        }

        private void TickGrid()
        {
            this.SubscribeToBattleState();
            if (this.isVisible)
            {
                this.CloseWhenEscapeIsPressed();
                return;
            }

            this.OpenWhenAlphaVoidCardIsClicked();
        }

        protected virtual void OnDestroy()
        {
            this.DisposeGrid();
        }

        private void DisposeGrid()
        {
            this.UnregisterCallbacks();
            this.UnsubscribeFromCardHoverEvents();
            this.UnsubscribeFromBattleState();
            this.hoveredCard = null;
        }

        private void BindElements(VisualElement root)
        {
            this.overlay = root?.Q("VoiceGridOverlay");
            this.panel = root?.Q("VoiceGridPanel");
            this.content = root?.Q("VoiceGridContent");
            this.alphaVoidCountLabel = root?.Q<Label>("AlphaTheVoidCountLabel");
            this.titleLabel = root?.Q<Label>("VoiceGridTitle");
            this.pageLabel = root?.Q<Label>("VoiceGridPageLabel");
            this.closeButton = root?.Q<Button>("VoiceGridCloseButton");
            this.previousButton = root?.Q<Button>("VoiceGridPreviousButton");
            this.nextButton = root?.Q<Button>("VoiceGridNextButton");
        }

        private void RegisterCallbacks()
        {
            this.alphaVoidCountLabel?.RegisterCallback<ClickEvent>(this.OnAlphaVoidCountClicked);
            this.closeButton?.RegisterCallback<ClickEvent>(this.OnCloseClicked);
            this.previousButton?.RegisterCallback<ClickEvent>(this.OnPreviousClicked);
            this.nextButton?.RegisterCallback<ClickEvent>(this.OnNextClicked);
            this.overlay?.RegisterCallback<ClickEvent>(this.OnOverlayClicked);
            this.panel?.RegisterCallback<ClickEvent>(this.OnPanelClicked);
        }

        private void UnregisterCallbacks()
        {
            this.alphaVoidCountLabel?.UnregisterCallback<ClickEvent>(this.OnAlphaVoidCountClicked);
            this.closeButton?.UnregisterCallback<ClickEvent>(this.OnCloseClicked);
            this.previousButton?.UnregisterCallback<ClickEvent>(this.OnPreviousClicked);
            this.nextButton?.UnregisterCallback<ClickEvent>(this.OnNextClicked);
            this.overlay?.UnregisterCallback<ClickEvent>(this.OnOverlayClicked);
            this.panel?.UnregisterCallback<ClickEvent>(this.OnPanelClicked);
        }

        private void SubscribeToCardHoverEvents()
        {
            Card3DCtrl.HoverEntered += this.OnCardHoverEntered;
            Card3DCtrl.HoverExited += this.OnCardHoverExited;
        }

        private void UnsubscribeFromCardHoverEvents()
        {
            Card3DCtrl.HoverEntered -= this.OnCardHoverEntered;
            Card3DCtrl.HoverExited -= this.OnCardHoverExited;
        }

        private void SubscribeToBattleState()
        {
            BattleState battleState = this.battleStateCtrl?.BattleState;
            if (battleState == this.subscribedBattleState) return;
            this.UnsubscribeFromBattleState();
            if (battleState == null) return;
            this.subscribedBattleState = battleState;
            this.subscribedBattleState.OnBattleStatusChanged += this.OnBattleStatusChanged;
        }

        private void UnsubscribeFromBattleState()
        {
            if (this.subscribedBattleState == null) return;
            this.subscribedBattleState.OnBattleStatusChanged -= this.OnBattleStatusChanged;
            this.subscribedBattleState = null;
        }

        private void OnAlphaVoidCountClicked(ClickEvent _)
        {
            this.Show();
        }

        private void OnCloseClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            this.Hide();
        }

        private void OnPreviousClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            if (this.currentPage <= 0) return;
            this.currentPage--;
            this.Refresh();
        }

        private void OnNextClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            int pageCount = this.GetPageCount(this.GetAlphaVoidCards().Count);
            if (this.currentPage >= pageCount - 1) return;
            this.currentPage++;
            this.Refresh();
        }

        private void OnOverlayClicked(ClickEvent evt)
        {
            if (evt.target != this.overlay) return;
            this.Hide();
        }

        private void OnPanelClicked(ClickEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnCardHoverEntered(Card3DCtrl card)
        {
            this.hoveredCard = card;
        }

        private void OnCardHoverExited(Card3DCtrl card)
        {
            if (this.hoveredCard != card) return;
            this.hoveredCard = null;
        }

        private void OnBattleStatusChanged()
        {
            if (!this.isVisible) return;
            this.Refresh();
        }

        private void OpenWhenAlphaVoidCardIsClicked()
        {
            if (Mouse.current?.leftButton.wasPressedThisFrame != true) return;
            if (this.hoveredCard == null) return;
            if (this.hoveredCard.CardOwner != Owner.alpha) return;
            if (this.hoveredCard.Location != Location.in_void) return;
            this.Show();
        }

        private void CloseWhenEscapeIsPressed()
        {
            if (Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
            this.Hide();
        }

        private void Show()
        {
            if (this.overlay == null) return;
            this.isVisible = true;
            this.currentPage = 0;
            this.overlay.RemoveFromClassList("voice-grid-overlay--hidden");
            this.Refresh();
        }

        private void Hide()
        {
            this.isVisible = false;
            this.overlay?.AddToClassList("voice-grid-overlay--hidden");
        }

        private void Refresh()
        {
            if (this.content == null) return;
            List<BattleCardSlot> cards = this.GetAlphaVoidCards();
            int pageCount = this.GetPageCount(cards.Count);
            this.currentPage = Mathf.Clamp(this.currentPage, 0, pageCount - 1);
            this.UpdateHeader(cards.Count);
            this.UpdatePagination(pageCount);
            this.BuildPage(cards);
        }

        private List<BattleCardSlot> GetAlphaVoidCards()
        {
            List<BattleCardSlot> cards = new List<BattleCardSlot>();
            BattleCardSlot[] slots = this.battleStateCtrl?.BattleState?.AlphaTheVoid;
            if (slots == null) return cards;
            foreach (BattleCardSlot slot in slots)
            {
                if (slot != null) cards.Add(slot);
            }
            cards.Sort((left, right) => left.slot_index.CompareTo(right.slot_index));
            return cards;
        }

        private int GetPageCount(int cardCount)
        {
            int pageSize = this.Columns * this.Rows;
            return Mathf.Max(1, Mathf.CeilToInt(cardCount / (float)pageSize));
        }

        private void UpdateHeader(int cardCount)
        {
            if (this.titleLabel == null) return;
            this.titleLabel.text = $"Alpha - The Void ({cardCount})";
        }

        private void UpdatePagination(int pageCount)
        {
            if (this.pageLabel != null) this.pageLabel.text = $"Page {this.currentPage + 1} / {pageCount}";
            this.previousButton?.SetEnabled(this.currentPage > 0);
            this.nextButton?.SetEnabled(this.currentPage < pageCount - 1);
        }

        private void BuildPage(List<BattleCardSlot> cards)
        {
            this.content.Clear();
            if (cards.Count == 0)
            {
                Label emptyLabel = new Label("Alpha's Void is empty.");
                emptyLabel.AddToClassList("voice-grid-empty-label");
                this.content.Add(emptyLabel);
                return;
            }

            int pageStart = this.currentPage * this.Columns * this.Rows;
            for (int rowIndex = 0; rowIndex < this.Rows; rowIndex++)
            {
                VisualElement row = new VisualElement();
                row.AddToClassList("voice-grid-row");
                this.content.Add(row);
                this.BuildRow(row, cards, pageStart + rowIndex * this.Columns);
            }
        }

        private void BuildRow(VisualElement row, List<BattleCardSlot> cards, int rowStart)
        {
            for (int columnIndex = 0; columnIndex < this.Columns; columnIndex++)
            {
                int cardIndex = rowStart + columnIndex;
                if (cardIndex < cards.Count)
                {
                    row.Add(this.BuildCard(cards[cardIndex]));
                    continue;
                }

                VisualElement placeholder = new VisualElement();
                placeholder.AddToClassList("voice-grid-card-placeholder");
                this.ApplyCardSize(placeholder);
                row.Add(placeholder);
            }
        }

        private VisualElement BuildCard(BattleCardSlot slot)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("voice-grid-card");
            this.ApplyCardSize(card);

            string cardName = string.IsNullOrWhiteSpace(slot.item_definition_name)
                ? slot.item_definition_code_name
                : slot.item_definition_name;
            if (string.IsNullOrWhiteSpace(cardName)) cardName = "Unknown Card";

            Label nameLabel = new Label(cardName);
            nameLabel.AddToClassList("voice-grid-card-name");
            card.Add(nameLabel);

            if (!string.IsNullOrWhiteSpace(slot.item_definition_code_name))
            {
                Label codeLabel = new Label(slot.item_definition_code_name);
                codeLabel.AddToClassList("voice-grid-card-code");
                card.Add(codeLabel);
            }

            Label statsLabel = new Label($"DEF {slot.final_def}   DMG {slot.total_damage_received}");
            statsLabel.AddToClassList("voice-grid-card-stats");
            card.Add(statsLabel);
            return card;
        }

        private void ApplyCardSize(VisualElement card)
        {
            card.style.width = this.CardWidth;
            card.style.height = this.CardHeight;
        }
    }
}
