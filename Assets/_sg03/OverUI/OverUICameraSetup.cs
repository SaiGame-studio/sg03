using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace SG03
{
    // Renders 3D objects (layer "OverUI") ON TOP of UI Toolkit panels.
    //
    // Approach:
    //   1. overUICamera renders only the "OverUI" layer into a RenderTexture.
    //   2. A Screen Space - Overlay Canvas + RawImage is created at runtime.
    //   3. Screen Space Overlay renders AFTER all cameras AND after UI Toolkit,
    //      so the RawImage is always on top regardless of sort order.
    //   4. RawImage uses alpha blending: transparent RT pixels = invisible,
    //      so the main UI shows through wherever there is no 3D object.
    //
    // Setup (one-time):
    //   1. Project Settings > Tags and Layers — add a layer named "OverUI".
    //   2. Create a Camera — assign it to Over UI Camera in Inspector.
    //   3. Add this component to any active GameObject.
    //   4. Click "Configure Camera & Apply" in the Inspector.
    //   5. Set any 3D object's Layer to "OverUI" — it appears above all UI.
    [AddComponentMenu("SG03/Over UI Camera Setup")]
    public class OverUICameraSetup : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Camera that renders only the OverUI layer into a RenderTexture.")]
        [SerializeField] private Camera overUICamera;

        [Header("Render Texture")]
        [SerializeField] private int rtWidth  = 1920;
        [SerializeField] private int rtHeight = 1080;

        [Header("Overlay Canvas")]
        [Tooltip("Sorting order of the auto-created overlay canvas.")]
        [SerializeField] private int overlaySortOrder = 100;

        private RenderTexture renderTexture;
        private GameObject    canvasGO;
        private RawImage      rawImage;

        private void Awake()
        {
            this.EnsureRenderTexture();
            if (this.overUICamera != null)
                this.overUICamera.targetTexture = this.renderTexture;
            this.CreateOverlayCanvas();
        }

        // Called by the Editor button.
        public void Apply()
        {
            this.EnsureRenderTexture();
            if (this.overUICamera != null)
                this.overUICamera.targetTexture = this.renderTexture;
            this.CreateOverlayCanvas();
        }

        // Configures the overUICamera so it only renders the OverUI layer into a
        // RenderTexture and does not composite to the screen directly.
        public void ConfigureCamera()
        {
            if (this.overUICamera == null) return;

            int layer = LayerMask.NameToLayer(OverUILayer.Name);
            if (layer < 0)
            {
                Debug.LogError($"[OverUICameraSetup] Layer \"{OverUILayer.Name}\" not found. "
                    + "Add it in Project Settings > Tags and Layers first.");
                return;
            }

            this.overUICamera.cullingMask     = 1 << layer;
            this.overUICamera.clearFlags      = CameraClearFlags.SolidColor;
            this.overUICamera.backgroundColor = new Color(0f, 0f, 0f, 0f);

            // Must be Base — an Overlay camera ignores targetTexture and composites
            // directly to screen, which corrupts the main camera output.
            UniversalAdditionalCameraData cameraData =
                this.overUICamera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
                cameraData.renderType = CameraRenderType.Base;

            // Remove from Main Camera's stack so it does not composite to screen.
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam != this.overUICamera)
            {
                UniversalAdditionalCameraData mainData =
                    mainCam.GetUniversalAdditionalCameraData();
                if (mainData != null)
                    mainData.cameraStack.Remove(this.overUICamera);
            }

            this.Apply();
            Debug.Log("[OverUICameraSetup] Camera configured and RenderTexture applied.");
        }

        // Creates a Screen Space - Overlay Canvas + RawImage at runtime.
        // Screen Space Overlay renders after ALL cameras and UI Toolkit panels,
        // so the RawImage is guaranteed to appear on top.
        private void CreateOverlayCanvas()
        {
            if (this.canvasGO != null) return;

            this.canvasGO = new GameObject("[OverUI] Canvas");
            this.canvasGO.transform.SetParent(this.transform, false);

            Canvas canvas = this.canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = this.overlaySortOrder;

            // RawImage fills the full screen and displays the RenderTexture.
            // Transparent RT pixels (alpha=0) are invisible, letting the main UI show through.
            GameObject rawGO = new GameObject("[OverUI] RawImage");
            rawGO.transform.SetParent(this.canvasGO.transform, false);

            this.rawImage         = rawGO.AddComponent<RawImage>();
            this.rawImage.texture = this.renderTexture;
            this.rawImage.color   = Color.white;

            RectTransform rt = rawGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Debug.Log($"[OverUICameraSetup] Overlay canvas created. sortOrder={this.overlaySortOrder}");
        }

        private void EnsureRenderTexture()
        {
            if (this.renderTexture != null
                && this.renderTexture.width  == this.rtWidth
                && this.renderTexture.height == this.rtHeight)
                return;

            if (this.renderTexture != null)
                this.renderTexture.Release();

            this.renderTexture = new RenderTexture(this.rtWidth, this.rtHeight, 24,
                RenderTextureFormat.ARGB32);
            this.renderTexture.antiAliasing = 1;
            this.renderTexture.Create();

            if (this.rawImage != null)
                this.rawImage.texture = this.renderTexture;
        }

        private void OnDestroy()
        {
            if (this.renderTexture != null)
                this.renderTexture.Release();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            this.rtWidth  = Screen.width  > 0 ? Screen.width  : 1920;
            this.rtHeight = Screen.height > 0 ? Screen.height : 1080;
        }
#endif
    }
}
