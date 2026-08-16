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

        // ─── Read-only accessors ──────────────────────────────────────────────────

        public Texture2D FrameTexture     => frameTexture;
        public Texture2D CharacterTexture => characterTexture;
        public Texture2D BackTexture      => backTexture;

    }
}
