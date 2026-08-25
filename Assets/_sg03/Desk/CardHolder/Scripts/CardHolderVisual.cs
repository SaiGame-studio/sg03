using SaiGame.Services;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Swaps the material on the CardHolder's MeshRenderer
    /// based on hover / selected state driven by <see cref="CardHolderCtrl"/> events.
    /// Attach to the same GameObject as <see cref="CardHolderCtrl"/>.
    /// </summary>
    [AddComponentMenu("SG03/CardHolder/Card Holder Visual")]
    [RequireComponent(typeof(CardHolderCtrl))]
    public class CardHolderVisual : SaiBehaviour
    {
        // ─── Linked components ────────────────────────────────────────────────────

        [Header("Linked Components")]
        [SerializeField] private CardHolderCtrl ctrl;
        [SerializeField] private MeshRenderer   meshRenderer;

        [Header("Visual")]
        [SerializeField] private bool showVisual = true;

        // ─── Alpha materials ──────────────────────────────────────────────────────

        [Header("Alpha Materials")]
        [SerializeField] private Material alphaDefault;
        [SerializeField] private Material alphaHover;
        [SerializeField] private Material alphaSelected;

        // ─── Omega materials ──────────────────────────────────────────────────────

        [Header("Omega Materials")]
        [SerializeField] private Material omegaDefault;
        [SerializeField] private Material omegaHover;
        [SerializeField] private Material omegaSelected;

        // ─── SaiBehaviour overrides ───────────────────────────────────────────────

        protected override void LoadComponents()
        {
            base.LoadComponents();
            this.LoadCtrl();
            this.LoadMeshRenderer();
            this.LoadAlphaDefault();
            this.LoadAlphaHover();
            this.LoadAlphaSelected();
            this.LoadOmegaDefault();
            this.LoadOmegaHover();
            this.LoadOmegaSelected();
        }

        protected override void ResetValue()
        {
            this.SetMaterial(this.ResolveDefault());
            this.ApplyVisualToggleInEditor();
        }

        protected virtual void LoadCtrl()
        {
            if (this.ctrl != null) return;
            this.ctrl = this.GetComponent<CardHolderCtrl>();
            Debug.LogWarning(this.transform.name + ": LoadCtrl", this.gameObject);
        }

        protected virtual void LoadMeshRenderer()
        {
            if (this.meshRenderer != null) return;
            this.meshRenderer = this.GetComponent<MeshRenderer>();
            Debug.LogWarning(this.transform.name + ": LoadMeshRenderer", this.gameObject);
        }

        private void LoadAlphaDefault()
        {
            if (this.alphaDefault != null) return;
#if UNITY_EDITOR
            this.alphaDefault = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/CardHolder/Materials/Alpha/AlphaCardHolderDefault.mat");
            if (this.alphaDefault == null)
                Debug.LogWarning("CardHolderVisual: AlphaDefault material not found", this);
#endif
        }

        private void LoadAlphaHover()
        {
            if (this.alphaHover != null) return;
#if UNITY_EDITOR
            this.alphaHover = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/CardHolder/Materials/Alpha/AlphaCardHolderHover.mat");
            if (this.alphaHover == null)
                Debug.LogWarning("CardHolderVisual: AlphaHover material not found", this);
#endif
        }

        private void LoadAlphaSelected()
        {
            if (this.alphaSelected != null) return;
#if UNITY_EDITOR
            this.alphaSelected = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/CardHolder/Materials/Alpha/AlphaCardHolderSelected.mat");
            if (this.alphaSelected == null)
                Debug.LogWarning("CardHolderVisual: AlphaSelected material not found", this);
#endif
        }

        private void LoadOmegaDefault()
        {
            if (this.omegaDefault != null) return;
#if UNITY_EDITOR
            this.omegaDefault = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/CardHolder/Materials/Omega/OmegaCardHolderDefault.mat");
            if (this.omegaDefault == null)
                Debug.LogWarning("CardHolderVisual: OmegaDefault material not found", this);
#endif
        }

        private void LoadOmegaHover()
        {
            if (this.omegaHover != null) return;
#if UNITY_EDITOR
            this.omegaHover = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/CardHolder/Materials/Omega/OmegaCardHolderHover.mat");
            if (this.omegaHover == null)
                Debug.LogWarning("CardHolderVisual: OmegaHover material not found", this);
#endif
        }

        private void LoadOmegaSelected()
        {
            if (this.omegaSelected != null) return;
#if UNITY_EDITOR
            this.omegaSelected = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_sg03/CardHolder/Materials/Omega/OmegaCardHolderSelected.mat");
            if (this.omegaSelected == null)
                Debug.LogWarning("CardHolderVisual: OmegaSelected material not found", this);
#endif
        }

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        protected override void Start()
        {
            this.SetMaterial(this.ResolveDefault());
        }

        private void OnEnable()  => this.Subscribe();
        private void OnDisable() => this.Unsubscribe();

        // ─── Event subscription ───────────────────────────────────────────────────

        private void Subscribe()
        {
            CardHolderCtrl.HoverEntered  += this.OnHoverEntered;
            CardHolderCtrl.HoverExited   += this.OnHoverExited;
            CardHolderCtrl.HolderSelected += this.OnSelected;
        }

        private void Unsubscribe()
        {
            CardHolderCtrl.HoverEntered  -= this.OnHoverEntered;
            CardHolderCtrl.HoverExited   -= this.OnHoverExited;
            CardHolderCtrl.HolderSelected -= this.OnSelected;
        }

        // ─── Event handlers ───────────────────────────────────────────────────────

        private void OnHoverEntered(CardHolderCtrl holder) => this.ApplyMaterialIfOwned(holder, this.ResolveHover());
        private void OnHoverExited(CardHolderCtrl holder)  => this.ResetMaterialIfOwned(holder);
        private void OnSelected(CardHolderCtrl holder)     => this.ApplyMaterialIfOwned(holder, this.ResolveSelected());

        // ─── Material helpers ─────────────────────────────────────────────────────

        private void ApplyMaterialIfOwned(CardHolderCtrl holder, Material mat)
        {
            if (holder != this.ctrl) return;
            this.SetMaterial(mat);
        }

        private void ResetMaterialIfOwned(CardHolderCtrl holder)
        {
            if (holder != this.ctrl) return;
            this.SetMaterial(this.ResolveDefault());
        }

#if UNITY_EDITOR
        private void OnValidate() => this.ApplyVisualToggleInEditor();
#endif

        private void ApplyVisualToggleInEditor()
        {
#if UNITY_EDITOR
            MeshRenderer targetRenderer = this.meshRenderer != null
                ? this.meshRenderer
                : this.GetComponent<MeshRenderer>();
            if (targetRenderer != null) targetRenderer.enabled = this.showVisual;
#endif
        }

        // ─── Material resolve ─────────────────────────────────────────────────────

        private Material ResolveDefault()  => this.ctrl.HolderOwner == Owner.alpha ? this.alphaDefault  : this.omegaDefault;
        private Material ResolveHover()    => this.ctrl.HolderOwner == Owner.alpha ? this.alphaHover    : this.omegaHover;
        private Material ResolveSelected() => this.ctrl.HolderOwner == Owner.alpha ? this.alphaSelected : this.omegaSelected;

        private void SetMaterial(Material mat)
        {
            if (this.meshRenderer == null) return;
            this.meshRenderer.sharedMaterial = mat;
        }
    }
}
