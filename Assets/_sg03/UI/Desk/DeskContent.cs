using System;
using SaiGame.Services;
using SG03;
using UnityEngine;
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

        // ─── Events ───────────────────────────────────────────────────────────────

        public event Action OnCardViewerShown;
        public event Action OnCardViewerHidden;

        // ─── Private state ────────────────────────────────────────────────────────

        private DeskContentUI ui;

        // ─── Public API ───────────────────────────────────────────────────────────

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
            ui.OnCardViewerHidden  += () => OnCardViewerHidden?.Invoke();
            ui.OnCardViewRequested += item =>
            {
                CardBaseStats stats       = ParseBaseStats(item?.definition?.base_stats);
                string        rawMetadata = item?.definition?.metadata;
                string        description = item?.definition?.ParsedMetadata?.description;
                Debug.Log($"[DeskContent] base_stats={item?.definition?.base_stats} | metadata={rawMetadata} | description={description}");
                cardReviewCtrl?.RequestShow(
                    item?.definition?.item_code,
                    item?.definition?.name,
                    stats,
                    description);
            };
        }
    }
}
