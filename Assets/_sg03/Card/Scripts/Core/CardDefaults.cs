using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Holds the default frame and back textures shared across all cards.
    /// Individual CardData assets can override these by assigning their own textures.
    /// When a CardData field is null, Card3D falls back to the values here.
    ///
    /// Create via: Assets > Create > SG03 > Card > Card Defaults
    /// Assign the asset to CardDataManager in the scene (auto-filled on Reset).
    /// </summary>
    [CreateAssetMenu(fileName = "CardDefaults", menuName = "SG03/Card/Card Defaults")]
    public class CardDefaults : ScriptableObject
    {
        [Tooltip("Default frame PNG used for all cards that do not override it.")]
        [SerializeField] private Texture2D frameTexture;

        [Tooltip("Default card back image used for all cards that do not override it.")]
        [SerializeField] private Texture2D backTexture;

        // ─── Read-only accessors ──────────────────────────────────────────────────

        public Texture2D FrameTexture => frameTexture;
        public Texture2D BackTexture  => backTexture;
    }
}
