using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SaiGame.Services;
using SG03;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace SG03.UI
{
    /// <summary>
    /// MonoBehaviour controller for the Desk content panel.
    /// Owns the <see cref="DeskContentUI"/> instance and wires events to the scene.
    /// Assign in the Inspector via the Lobby scene hierarchy so it is visible and editable.
    /// </summary>
    [AddComponentMenu("SG03/UI/Desk Content")]
    public class DeskContent : SaiBehaviour
    {
        [Header("Assets")]
        [Tooltip("DeskContent.uxml — the visual tree to instantiate inside the content area.")]
        [SerializeField] private VisualTreeAsset contentAsset;

        [Header("Card 3D Review")]
        [Tooltip("Controller that drives the 3D card preview in the scene.")]
        [SerializeField] private Card3DReviewCtrl cardReviewCtrl;

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadContentAsset();
            this.LoadCardReviewCtrl();
        }

        private void LoadContentAsset()
        {
            if (this.contentAsset != null) return;
#if UNITY_EDITOR
            this.contentAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/_sg03/UI/Desk/DeskContent.uxml");
            Debug.LogWarning(transform.name + ": LoadContentAsset", gameObject);
#endif
        }

        private void LoadCardReviewCtrl()
        {
            if (this.cardReviewCtrl != null) return;
            this.cardReviewCtrl = FindFirstObjectByType<Card3DReviewCtrl>(FindObjectsInactive.Include);
            Debug.LogWarning(transform.name + ": LoadCardReviewCtrl", gameObject);
        }

        // Parses the raw base_stats JSON string from the server into a CardBaseStats object.
        // Returns null if the string is null or empty.
        private static CardBaseStats ParseBaseStats(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonUtility.FromJson<CardBaseStats>(json);
        }

        private static CardDefinitionData ToCardDefinition(ItemDefinitionData definition)
        {
            if (definition == null) return null;

            return new CardDefinitionData
            {
                id = definition.id,
                item_code = definition.item_code,
                name = definition.name,
                description = definition.description,
                category = definition.category,
                rarity = definition.rarity,
                grid_width = definition.grid_width,
                grid_height = definition.grid_height,
                is_stackable = definition.is_stackable,
                allow_client_update_qty = definition.allow_client_update_qty,
                client_writable = definition.client_writable,
                game_id = definition.game_id,
                base_stats = ParseDefinitionStats(definition.base_stats),
                metadata = string.IsNullOrEmpty(definition.metadata)
                    ? null
                    : JsonUtility.FromJson<CardDefinitionMetadata>(definition.metadata)
            };
        }

        private static Dictionary<string, JToken> ParseDefinitionStats(string statsJson)
        {
            if (string.IsNullOrEmpty(statsJson)) return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, JToken>>(statsJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action OnCardViewerShown;
        public event Action OnCardViewerHidden;

        // ─── Private state ────────────────────────────────────────────────────────

        private DeskContentUI ui;
        private bool isCardViewerClosing;
        private Action cardViewerClosedContinuation;

        // ─── Public API ───────────────────────────────────────────────────────────

        private void Update() => this.HandleCloseViewerOnEscape();

        /// <summary>
        /// Instantiates the desk content UXML into <paramref name="container"/> and
        /// initialises the UI logic. The caller is responsible for clearing the
        /// container first.
        /// </summary>
        public void Show(VisualElement container)
        {
            if (contentAsset == null) return;

            TemplateContainer content = contentAsset.Instantiate();
            content.style.flexGrow   = 1;
            content.style.flexShrink = 1;
            content.style.width      = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.height     = new StyleLength(new Length(100, LengthUnit.Percent));
            content.style.alignSelf  = Align.Stretch;
            container.Add(content);

            ui = new DeskContentUI(content);
            ui.OnCardViewerShown   += () => OnCardViewerShown?.Invoke();
            ui.OnCardViewerHidden  += this.HandleCardViewerHidden;
            ui.OnCardViewRequested += item =>
            {
                ItemDefinitionData definition = item?.definition;
                CardDefinitionData previewDefinition = ToCardDefinition(definition);
                CardBaseStats stats       = ParseBaseStats(definition?.base_stats);
                string        rawMetadata = definition?.metadata;
                string        description = CardDescriptionTemplateResolver.Resolve(
                    previewDefinition?.description,
                    previewDefinition);
                string        cardType    = previewDefinition?.metadata?.type;
                Debug.Log($"[DeskContent] base_stats={definition?.base_stats} | metadata={rawMetadata} | description={description}");
                cardReviewCtrl?.RequestShow(
                    definition?.item_code,
                    definition?.name,
                    stats,
                    description,
                    cardType);
            };
        }

        private void HandleCloseViewerOnEscape()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (!keyboard.escapeKey.wasPressedThisFrame) return;
            if (this.ui == null) return;

            this.ui.RequestCloseViewerFromDimLayer();
        }

        /// <summary>
        /// Closes the active card review and invokes <paramref name="continuation"/>
        /// only after the card has finished returning to its origin position.
        /// </summary>
        public void CloseCardViewerBefore(Action continuation)
        {
            if (this.isCardViewerClosing)
            {
                this.cardViewerClosedContinuation = continuation;
                return;
            }

            if (this.ui == null || !this.ui.IsDimLayerVisible)
            {
                continuation?.Invoke();
                return;
            }

            this.cardViewerClosedContinuation = continuation;
            this.isCardViewerClosing = true;
            this.ui.RequestCloseViewerFromDimLayer();
        }

        private void HandleCardViewerHidden()
        {
            this.isCardViewerClosing = true;

            if (this.cardReviewCtrl == null)
            {
                this.CompleteCardViewerHide();
                return;
            }

            this.cardReviewCtrl.Hide(this.CompleteCardViewerHide);
        }

        private void CompleteCardViewerHide()
        {
            this.isCardViewerClosing = false;
            OnCardViewerHidden?.Invoke();

            Action continuation = this.cardViewerClosedContinuation;
            this.cardViewerClosedContinuation = null;
            continuation?.Invoke();
        }
    }
}
