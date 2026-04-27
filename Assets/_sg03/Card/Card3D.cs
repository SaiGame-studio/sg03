using System.Collections;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// 3D card component. Attach to any GameObject that represents a physical card.
    ///
    /// Expected child hierarchy (created automatically via "Setup Card Structure"):
    ///   CardObject  ← this component lives here
    ///   ├── FrontFace
    ///   │   ├── Character   (background quad — character artwork)
    ///   │   └── Frame       (foreground quad — transparent frame PNG)
    ///   └── BackFace        (rotated 180° on Y)
    ///       └── Back        (quad — card back image)
    ///
    /// Flip mechanics rotate this root transform on the Y axis.
    /// </summary>
    [AddComponentMenu("SG03/Card/Card 3D")]
    public class Card3D : MonoBehaviour
    {
        [Header("Face Renderers")]
        [Tooltip("Renderer for the transparent frame PNG overlaid on the front face.")]
        [SerializeField] private Renderer frontFrameRenderer;

        [Tooltip("Renderer for the character artwork (background layer of the front face).")]
        [SerializeField] private Renderer characterRenderer;

        [Tooltip("Renderer for the card back image.")]
        [SerializeField] private Renderer backRenderer;

        [Header("Card Data")]
        [Tooltip("ScriptableObject that provides the frame, character, and back textures.")]
        [SerializeField] private CardData     cardData;

        [Tooltip("Shared default textures used as fallback when CardData leaves frame or back null.")]
        [SerializeField] private CardDefaults cardDefaults;

        [Header("Flip Animation")]
        [Tooltip("Duration of the flip animation in seconds.")]
        [SerializeField] private float flipDuration = 0.4f;

        [Header("Card Size")]
        [Tooltip("Card width in pixels (converted to world units via Pixels Per Unit).")]
        [SerializeField] private int   cardWidthPixels  = 750;
        [Tooltip("Card height in pixels (converted to world units via Pixels Per Unit).")]
        [SerializeField] private int   cardHeightPixels = 1050;
        [Tooltip("How many pixels equal 1 Unity world unit. Match your texture import setting.")]
        [SerializeField] private float pixelsPerUnit    = 100f;

        private float CardWidth  => cardWidthPixels  / pixelsPerUnit;
        private float CardHeight => cardHeightPixels / pixelsPerUnit;

        private bool      isFacingFront = true;
        private float     currentYAngle = 0f;
        private Coroutine flipCoroutine;

        // ─── Unity lifecycle ──────────────────────────────────────────────────────

        private void Start() => ApplyTextures();

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Scales all face quads (Character, Frame, Back) to match the pixel size
        /// converted to world units via Pixels Per Unit.
        /// </summary>
        public void ApplySize()
        {
            Vector3 size = new Vector3(CardWidth, CardHeight, 1f);
            if (frontFrameRenderer != null) frontFrameRenderer.transform.localScale = size;
            if (characterRenderer  != null) characterRenderer.transform.localScale  = size;
            if (backRenderer       != null) backRenderer.transform.localScale       = size;
        }

        /// <summary>
        /// Applies only the CardDefaults textures (frame + back) directly to the renderers.
        /// Used in Editor setup when no CardData is assigned yet.
        /// </summary>
        public void ApplyDefaults()
        {
            if (cardDefaults == null) return;
            SetRendererTexture(frontFrameRenderer, cardDefaults.FrameTexture);
            SetRendererTexture(backRenderer,       cardDefaults.BackTexture);
        }

        /// <summary>
        /// Pushes the textures from the assigned CardData to their respective renderer
        /// material instances. Optional <paramref name="defaults"/> fills in any null
        /// frame or back texture in CardData.
        /// </summary>
        public void ApplyTextures()
        {
            if (cardData == null)
            {
                Debug.LogWarning($"[Card3D] No CardData assigned on '{name}'.", this);
                return;
            }

            Texture2D frame = cardData.FrameTexture != null ? cardData.FrameTexture : cardDefaults?.FrameTexture;
            Texture2D back  = cardData.BackTexture  != null ? cardData.BackTexture  : cardDefaults?.BackTexture;

            SetRendererTexture(frontFrameRenderer, frame);
            SetRendererTexture(characterRenderer,  cardData.CharacterTexture);
            SetRendererTexture(backRenderer,       back);
        }

        /// <summary>
        /// Assigns a new <see cref="CardData"/> and immediately applies its textures.
        /// Called by <see cref="CardDataManager"/> after an Addressables load completes.
        /// </summary>
        public void SetCardData(CardData data)
        {
            cardData = data;
            ApplyTextures();
        }

        /// <summary>Shows the front face immediately (no animation).</summary>
        public void ShowFront()
        {
            StopFlip();
            currentYAngle = 0f;
            transform.localEulerAngles = new Vector3(0f, currentYAngle, 0f);
            isFacingFront = true;
        }

        /// <summary>Shows the card back face immediately (no animation).</summary>
        public void ShowBack()
        {
            StopFlip();
            currentYAngle = 180f;
            transform.localEulerAngles = new Vector3(0f, currentYAngle, 0f);
            isFacingFront = false;
        }

        /// <summary>Flips the card with a smooth SmoothStep animation.</summary>
        public void Flip()
        {
            StopFlip();
            float targetY = isFacingFront ? 180f : 0f;
            isFacingFront = !isFacingFront;
            flipCoroutine = StartCoroutine(FlipRoutine(currentYAngle, targetY));
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private void StopFlip()
        {
            if (flipCoroutine == null) return;
            StopCoroutine(flipCoroutine);
            flipCoroutine = null;
        }

        private IEnumerator FlipRoutine(float fromY, float toY)
        {
            float elapsed = 0f;

            while (elapsed < flipDuration)
            {
                elapsed       += Time.deltaTime;
                float t        = Mathf.Clamp01(elapsed / flipDuration);
                currentYAngle  = Mathf.LerpAngle(fromY, toY, Mathf.SmoothStep(0f, 1f, t));
                transform.localEulerAngles = new Vector3(0f, currentYAngle, 0f);
                yield return null;
            }

            currentYAngle = toY;
            transform.localEulerAngles = new Vector3(0f, toY, 0f);
            flipCoroutine = null;
        }

        private static void SetRendererTexture(Renderer rend, Texture2D texture)
        {
            if (rend == null || texture == null) return;
            rend.material.mainTexture = texture;
        }
    }
}
