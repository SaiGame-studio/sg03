using SaiGame.Services;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Binds DeskContent.uxml to live data loaded by DeskList.
    // Manages navigation between the list panel and the detail panel.
    public class DeskContentUI
    {
        private readonly VisualElement deskRoot;
        private readonly VisualElement listPanel;
        private readonly ScrollView deskListView;
        private readonly VisualElement emptyState;
        private readonly VisualElement loadingState;
        private readonly VisualElement createForm;
        private readonly TextField deskNameField;
        private readonly Button newDeskBtn;
        private readonly Button refreshBtn;
        private readonly Button confirmCreateBtn;
        private readonly Button cancelCreateBtn;
        private readonly DeskList list;
        private readonly DeskDetailUI detailUI;
        private VisualElement dimLayer;
        private Button dimCloseBtn;

        public event System.Action OnCardViewerShown;
        public event System.Action OnCardViewerHidden;
        public event System.Action<InventoryItemData> OnCardViewRequested;

        public bool IsDimLayerVisible => this.dimLayer != null && !this.dimLayer.ClassListContains("desk-dim-layer--hidden");

        public DeskContentUI(VisualElement root)
        {
            this.deskRoot         = root.Q("DeskRoot");
            this.dimLayer         = this.deskRoot?.Q("DimLayer");
            this.dimCloseBtn      = this.deskRoot?.Q<Button>("DimCloseBtn");
            this.listPanel        = root.Q("ListPanel");
            this.deskListView     = root.Q<ScrollView>("DeskList");
            this.emptyState       = root.Q("EmptyState");
            this.loadingState     = root.Q("LoadingState");
            this.createForm       = root.Q("CreateForm");
            this.deskNameField    = root.Q<TextField>("DeskNameField");
            this.newDeskBtn       = root.Q<Button>("NewDeskBtn");
            this.refreshBtn       = root.Q<Button>("RefreshBtn");
            this.confirmCreateBtn = root.Q<Button>("ConfirmCreateBtn");
            this.cancelCreateBtn  = root.Q<Button>("CancelCreateBtn");

            this.list = new DeskList();
            this.list.OnDataUpdated += this.Render;

            this.detailUI = new DeskDetailUI(root, this.list);
            this.detailUI.OnBackRequested      += this.ShowListPanel;
            this.detailUI.OnCardViewerShown  += this.OnDetailCardViewerShown;
            this.detailUI.OnCardViewerHidden += this.OnDetailCardViewerHidden;
            this.detailUI.OnCardViewRequested += item =>
            {
                this.ShowDimLayer();
                this.OnCardViewRequested?.Invoke(item);
            };

            if (this.dimCloseBtn != null)
                this.dimCloseBtn.RegisterCallback<ClickEvent>(_ => this.OnDimLayerClicked());

            if (this.newDeskBtn != null)
                this.newDeskBtn.RegisterCallback<ClickEvent>(_ => this.ShowCreateForm());

            if (this.refreshBtn != null)
                this.refreshBtn.RegisterCallback<ClickEvent>(_ => this.DoRefresh());

            if (this.confirmCreateBtn != null)
                this.confirmCreateBtn.RegisterCallback<ClickEvent>(_ => this.OnConfirmCreate());

            if (this.cancelCreateBtn != null)
                this.cancelCreateBtn.RegisterCallback<ClickEvent>(_ => this.HideCreateForm());

            this.HideCreateForm();
            this.ShowLoading();
            this.DoRefresh();
        }

        // ── Immersive mode ────────────────────────────────────────────────────

        private void OnDetailCardViewerShown()
        {
            this.deskRoot?.AddToClassList("desk-root--immersive");
            this.OnCardViewerShown?.Invoke();
        }

        private void OnDetailCardViewerHidden()
        {
            this.deskRoot?.RemoveFromClassList("desk-root--immersive");
            this.HideDimLayer();
            this.OnCardViewerHidden?.Invoke();
        }

        private void ShowDimLayer() => this.dimLayer?.RemoveFromClassList("desk-dim-layer--hidden");

        private void HideDimLayer() => this.dimLayer?.AddToClassList("desk-dim-layer--hidden");

        private void OnDimLayerClicked()
        {
            this.HideDimLayer();
            this.OnCardViewerHidden?.Invoke();
        }

        public void RequestCloseViewerFromDimLayer()
        {
            if (!this.IsDimLayerVisible) return;
            this.OnDimLayerClicked();
        }

        // ── Panel navigation ──────────────────────────────────────────────────

        private void ShowListPanel()
        {
            this.detailUI.Hide();
            if (this.listPanel != null)
                this.listPanel.RemoveFromClassList("desk-panel--hidden");
        }

        private void ShowDetailPanel(PresetData desk)
        {
            if (this.listPanel != null)
                this.listPanel.AddToClassList("desk-panel--hidden");
            this.detailUI.Show(desk);
        }

        // ── Panel navigation ──────────────────────────────────────────────────

        private void DoRefresh()
        {
            this.ShowLoading();
            this.list.Refresh();
        }

        // ── Create form ───────────────────────────────────────────────────────

        private void ShowCreateForm()
        {
            if (this.createForm == null) return;
            if (this.deskNameField != null) this.deskNameField.value = string.Empty;
            this.createForm.style.display = DisplayStyle.Flex;
        }

        private void HideCreateForm()
        {
            if (this.createForm == null) return;
            this.createForm.style.display = DisplayStyle.None;
        }

        private void OnConfirmCreate()
        {
            string name = this.deskNameField != null ? this.deskNameField.value.Trim() : string.Empty;

            if (this.confirmCreateBtn != null) this.confirmCreateBtn.SetEnabled(false);

            this.list.CreateDesk(
                name: name,
                onSuccess: _ =>
                {
                    if (this.confirmCreateBtn != null) this.confirmCreateBtn.SetEnabled(true);
                    this.HideCreateForm();
                    this.DoRefresh();
                },
                onError: _ =>
                {
                    if (this.confirmCreateBtn != null) this.confirmCreateBtn.SetEnabled(true);
                }
            );
        }

        // ── State helpers ─────────────────────────────────────────────────────

        private void ShowLoading()
        {
            if (this.loadingState != null) this.loadingState.style.display  = DisplayStyle.Flex;
            if (this.emptyState   != null) this.emptyState.style.display    = DisplayStyle.None;
            if (this.deskListView != null) this.deskListView.style.display  = DisplayStyle.None;
        }

        private void ShowEmpty()
        {
            if (this.loadingState != null) this.loadingState.style.display  = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display    = DisplayStyle.Flex;
            if (this.deskListView != null) this.deskListView.style.display  = DisplayStyle.None;
        }

        private void ShowList()
        {
            if (this.loadingState != null) this.loadingState.style.display  = DisplayStyle.None;
            if (this.emptyState   != null) this.emptyState.style.display    = DisplayStyle.None;
            if (this.deskListView != null) this.deskListView.style.display  = DisplayStyle.Flex;
        }

        // ── Render ────────────────────────────────────────────────────────────

        private void Render()
        {
            if (this.deskListView == null) return;

            PresetData[] desks = this.list.Desks;
            if (desks == null || desks.Length == 0)
            {
                this.ShowEmpty();
                return;
            }

            this.deskListView.Clear();
            foreach (PresetData desk in desks)
                this.deskListView.Add(this.BuildDeskRow(desk));

            this.ShowList();
        }

        private VisualElement BuildDeskRow(PresetData desk)
        {
            VisualElement row = new VisualElement();
            row.name = $"DeskRow_{desk.id}";
            row.AddToClassList("desk-row");

            Label icon = new Label("D");
            icon.name = $"DeskRowIcon_{desk.id}";
            icon.AddToClassList("desk-row__icon");
            row.Add(icon);

            VisualElement info = new VisualElement();
            info.name = $"DeskRowInfo_{desk.id}";
            info.AddToClassList("desk-row__info");

            string displayName = string.IsNullOrEmpty(desk.name) ? "Unnamed Desk" : desk.name;
            Label nameLabel = new Label(displayName);
            nameLabel.name = $"DeskRowName_{desk.id}";
            nameLabel.AddToClassList("desk-row__name");
            info.Add(nameLabel);

            string presetType = string.IsNullOrEmpty(desk.preset_type) ? "card_desk" : desk.preset_type;
            Label metaLabel = new Label(presetType);
            metaLabel.name = $"DeskRowMeta_{desk.id}";
            metaLabel.AddToClassList("desk-row__meta");
            info.Add(metaLabel);

            row.Add(info);

            PresetData captured = desk;

            Button deleteBtn = new Button();
            deleteBtn.name = $"DeskRowDeleteBtn_{desk.id}";
            deleteBtn.text = "\uD83D\uDDD1";
            deleteBtn.AddToClassList("desk-row__delete-btn");
            deleteBtn.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                this.OnDeleteDesk(captured);
            });
            row.Add(deleteBtn);

            row.RegisterCallback<ClickEvent>(_ => this.ShowDetailPanel(captured));

            return row;
        }

        private void OnDeleteDesk(PresetData desk)
        {
            this.list.DeleteDesk(
                presetId: desk.id,
                onSuccess: () => this.DoRefresh(),
                onError: _ => { }
            );
        }
    }
}
