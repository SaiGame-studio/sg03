using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    [AddComponentMenu("SG03/UI/Modal Dim Layer")]
    public sealed class ModalDimLayer : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset dimLayerAsset;

        private static int activeInputBlockerCount;

        private TemplateContainer layerInstance;
        private VisualElement overlay;
        private bool isVisible;

        public static bool IsInputBlocked => activeInputBlockerCount > 0;
        public VisualElement Overlay => this.overlay;

        public bool Initialize(VisualElement parent, VisualElement modalContent)
        {
            if (parent == null || modalContent == null || this.dimLayerAsset == null)
            {
                Debug.LogError(this.name + ": ModalDimLayer requires a parent, modal content, and DimLayer asset.", this.gameObject);
                return false;
            }

            this.DisposeLayer();
            this.layerInstance = this.dimLayerAsset.Instantiate();
            this.StretchLayerInstanceToParent();
            this.overlay = this.layerInstance.Q("DimLayerOverlay");
            VisualElement contentHost = this.layerInstance.Q("DimLayerContent");
            if (this.overlay == null || contentHost == null)
            {
                Debug.LogError(this.name + ": DimLayer asset is missing DimLayerOverlay or DimLayerContent.", this.gameObject);
                this.DisposeLayer();
                return false;
            }

            parent.Add(this.layerInstance);
            contentHost.Add(modalContent);
            modalContent.style.display = DisplayStyle.Flex;
            this.layerInstance.style.display = DisplayStyle.None;
            return true;
        }

        private void StretchLayerInstanceToParent()
        {
            this.layerInstance.style.position = Position.Absolute;
            this.layerInstance.style.left = 0;
            this.layerInstance.style.right = 0;
            this.layerInstance.style.top = 0;
            this.layerInstance.style.bottom = 0;
        }

        public void Show()
        {
            if (this.layerInstance == null || this.isVisible) return;
            this.isVisible = true;
            activeInputBlockerCount++;
            this.layerInstance.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (!this.isVisible) return;
            this.isVisible = false;
            activeInputBlockerCount = Mathf.Max(0, activeInputBlockerCount - 1);
            if (this.layerInstance != null) this.layerInstance.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            this.DisposeLayer();
        }

        private void DisposeLayer()
        {
            this.Hide();
            this.layerInstance?.RemoveFromHierarchy();
            this.layerInstance = null;
            this.overlay = null;
        }
    }
}
