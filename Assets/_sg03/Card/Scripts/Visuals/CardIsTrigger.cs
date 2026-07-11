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

        protected override void LoadComponents()
        {
            base.LoadComponents();
            if (this.cardCtrl == null) this.cardCtrl = this.GetComponentInParent<Card3DCtrl>();
            if (this.triggerRenderer == null) this.triggerRenderer = this.GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            Card3DCtrl.TriggerStateChanged += this.OnTriggerStateChanged;
            if (this.cardCtrl != null)
            {
                this.SetVisual(this.cardCtrl.IsTrigger);
            }
        }

        private void OnDisable()
        {
            Card3DCtrl.TriggerStateChanged -= this.OnTriggerStateChanged;
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
