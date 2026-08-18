using TMPro;
using UnityEngine;

namespace SG03
{
    /// <summary>
    /// Holds the default card fronts and back texture shared across all cards.
    /// Individual CardData assets can override these by assigning their own textures.
    /// When a CardData field is null, Card3D falls back to the values here.
    ///
    /// Create via: Assets > Create > SG03 > Card > Card Defaults
    /// Assign the asset to CardDataManager in the scene (auto-filled on Reset).
    /// </summary>
    [CreateAssetMenu(fileName = "CardDefaults", menuName = "SG03/Card/Card Defaults")]
    public class CardDefaults : ScriptableObject
    {
        [Tooltip("Default card_front_char_1 PNG used for all cards that do not override it.")]
        [SerializeField] private Texture2D cardFrontChar1;

        [Tooltip("Default card_front_char_2 PNG.")]
        [SerializeField] private Texture2D cardFrontChar2;

        [Tooltip("Default card_front_ability_1 PNG.")]
        [SerializeField] private Texture2D cardFrontAbility1;

        [Tooltip("Default card back image used for all cards that do not override it.")]
        [SerializeField] private Texture2D backTexture;

        [Header("Card Text Fonts")]
        [Tooltip("Default font asset for the card name.")]
        [SerializeField] private TMP_FontAsset cardNameFont;

        [Tooltip("Default font asset for the ATK value.")]
        [SerializeField] private TMP_FontAsset atkFont;

        [Tooltip("Default font asset for the DEF value.")]
        [SerializeField] private TMP_FontAsset defFont;

        [Tooltip("Default font asset for the card description.")]
        [SerializeField] private TMP_FontAsset descriptionFont;

        // ─── Read-only accessors ──────────────────────────────────────────────────

        public Texture2D CardFrontChar1 => cardFrontChar1;
        public Texture2D CardFrontChar2 => cardFrontChar2;
        public Texture2D CardFrontAbility1 => cardFrontAbility1;
        public Texture2D BackTexture       => backTexture;
        public TMP_FontAsset CardNameFont  => cardNameFont;
        public TMP_FontAsset AtkFont       => atkFont;
        public TMP_FontAsset DefFont       => defFont;
        public TMP_FontAsset DescriptionFont => descriptionFont;
    }
}
