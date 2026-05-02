using UnityEngine;

namespace SG03
{
    /// <summary>
    /// ScriptableObject that holds all textures needed to render a 3D card.
    /// Create via: Assets > Create > SG03 > Card > Card Data
    /// Assign the asset to a Card3D component in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "SG03/Card/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Front Face")]
        [Tooltip("(Optional) Frame PNG for this card. Leave null to use the default from CardDefaults.")]
        [SerializeField] private Texture2D frameTexture;

        [Tooltip("Character artwork shown as the background of the front face. Required.")]
        [SerializeField] private Texture2D characterTexture;

        [Header("Back Face")]
        [Tooltip("(Optional) Card back image for this card. Leave null to use the default from CardDefaults.")]
        [SerializeField] private Texture2D backTexture;

        [Header("Card Info")]
        [Tooltip("Display name of the card.")]
        [SerializeField] private string cardName;

        [Tooltip("Attack value (0 or higher).")]
        [SerializeField] private int atk;

        [Tooltip("Defense value (0 or higher).")]
        [SerializeField] private int def;

        [Tooltip("Star level of the card (0–12).")]
        [SerializeField] private int stars;

        [Tooltip("Flavour or effect text shown in the card description box.")]
        [TextArea(2, 5)]
        [SerializeField] private string description;

        // ─── Read-only accessors ──────────────────────────────────────────────────

        public Texture2D FrameTexture     => frameTexture;
        public Texture2D CharacterTexture => characterTexture;
        public Texture2D BackTexture      => backTexture;

        public string CardName    => cardName;
        public int    Atk         => atk;
        public int    Def         => def;
        public int    Stars       => stars;
        public string Description => description;
    }
}
