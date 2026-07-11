using UnityEngine;
using SaiGame.Services;

namespace SG03
{
    [AddComponentMenu("SG03/Card/Card Is Trigger")]
    public class CardIsTrigger : SaiBehaviour
    {
        [SerializeField] private Card3DCtrl cardCtrl;
        [SerializeField] private Renderer triggerRenderer;
        
        private MaterialPropertyBlock propBlock;
        private static readonly int IsTriggerProp = Shader.PropertyToID("_IsTrigger");

        private ClientActions clientActions;

        protected override void LoadComponents()
        {
            base.LoadComponents();
            if (this.cardCtrl == null) this.cardCtrl = this.GetComponentInParent<Card3DCtrl>();
            if (this.triggerRenderer == null) this.triggerRenderer = this.GetComponent<Renderer>();
            if (this.clientActions == null) this.clientActions = UnityEngine.Object.FindFirstObjectByType<ClientActions>(FindObjectsInactive.Include);
        }

        private void OnEnable()
        {
            Card3DCtrl.TriggerStateChanged += this.OnTriggerStateChanged;
            Card3DCtrl.LocationChanged += this.OnLocationChanged;
            if (this.cardCtrl != null)
            {
                this.SetVisual(this.cardCtrl.IsTrigger);
                this.CheckAndEnableRenderer();
            }
        }

        private void OnDisable()
        {
            Card3DCtrl.TriggerStateChanged -= this.OnTriggerStateChanged;
            Card3DCtrl.LocationChanged -= this.OnLocationChanged;
            this.StopAllCoroutines();
        }

        private void OnLocationChanged(Card3DCtrl card, Location newLocation)
        {
            if (this.cardCtrl == null || card != this.cardCtrl) return;
            this.CheckAndEnableRenderer();
        }

        private void CheckAndEnableRenderer()
        {
            if (this.cardCtrl == null || this.triggerRenderer == null) return;
            this.StopAllCoroutines();
            
            if (this.cardCtrl.Location == Location.in_front)
            {
                this.StartCoroutine(this.WaitAndEnableRenderer());
            }
            else
            {
                this.SetRendererEnabled(false);
            }
        }

        private System.Collections.IEnumerator WaitAndEnableRenderer()
        {
            if (this.clientActions != null)
            {
                yield return new WaitUntil(() => !this.clientActions.IsResuming);
            }
            yield return new WaitUntil(() => !this.cardCtrl.IsAnimating);
            if (this.cardCtrl.Location == Location.in_front)
            {
                this.SetRendererEnabled(true);
            }
        }

        private void SetRendererEnabled(bool enable)
        {
            if (this.triggerRenderer != null && this.triggerRenderer.enabled != enable)
            {
                this.triggerRenderer.enabled = enable;
            }
        }

        private void OnTriggerStateChanged(Card3DCtrl card, bool isTrigger)
        {
            if (this.cardCtrl == null || card != this.cardCtrl) return;
            this.SetVisual(isTrigger);
        }

        private void SetVisual(bool isTrigger)
        {
            if (this.triggerRenderer != null)
            {
                if (this.propBlock == null) this.propBlock = new MaterialPropertyBlock();
                this.triggerRenderer.GetPropertyBlock(this.propBlock);
                this.propBlock.SetFloat(IsTriggerProp, isTrigger ? 1f : 0f);
                this.triggerRenderer.SetPropertyBlock(this.propBlock);
            }
        }
    }
}
