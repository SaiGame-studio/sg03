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

        [Header("Card Text Colors")]
        [SerializeField] private Color cardNameColor = new Color(0.3803922f, 0.24705884f, 0.14117648f, 1f);
        [SerializeField] private Color starsColor = Color.white;
        [SerializeField] private Color atkColor = Color.white;
        [SerializeField] private Color defColor = Color.white;
        [SerializeField] private Color descriptionColor = new Color(0.25490198f, 0.16862746f, 0.09803922f, 1f);

        [Header("Card Text Typography")]
        [SerializeField] private float cardNameFontSize = 3f;
        [SerializeField] private bool cardNameBold = true;
        [SerializeField] private float starsFontSize = 3f;
        [SerializeField] private bool starsBold;
        [SerializeField] private float atkFontSize = 3f;
        [SerializeField] private bool atkBold;
        [SerializeField] private float defFontSize = 3f;
        [SerializeField] private bool defBold;
        [SerializeField] private float descriptionFontSize = 3f;
        [SerializeField] private bool descriptionBold = true;

        // ─── Read-only accessors ──────────────────────────────────────────────────

        public Texture2D CardFrontChar1 => cardFrontChar1;
        public Texture2D CardFrontChar2 => cardFrontChar2;
        public Texture2D CardFrontAbility1 => cardFrontAbility1;
        public Texture2D BackTexture       => backTexture;
        public TMP_FontAsset CardNameFont  => cardNameFont;
        public TMP_FontAsset AtkFont       => atkFont;
        public TMP_FontAsset DefFont       => defFont;
        public TMP_FontAsset DescriptionFont => descriptionFont;
        public Color CardNameColor          => cardNameColor;
        public Color StarsColor             => starsColor;
        public Color AtkColor               => atkColor;
        public Color DefColor               => defColor;
        public Color DescriptionColor       => descriptionColor;
        public float CardNameFontSize        => cardNameFontSize;
        public bool CardNameBold             => cardNameBold;
        public float StarsFontSize           => starsFontSize;
        public bool StarsBold                => starsBold;
        public float AtkFontSize             => atkFontSize;
        public bool AtkBold                  => atkBold;
        public float DefFontSize             => defFontSize;
        public bool DefBold                  => defBold;
        public float DescriptionFontSize     => descriptionFontSize;
        public bool DescriptionBold          => descriptionBold;
    }
}
