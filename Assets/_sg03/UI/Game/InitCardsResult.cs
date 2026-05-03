namespace SG03.UI
{
    /// <summary>
    /// Data payload broadcast by BattleState.OnInitCards after the init_cards script completes.
    /// Carries the card-count changes that resulted from the initialization.
    /// </summary>
    public struct InitCardsResult
    {
        public int AlphaCardsAddedToHand;
        public int AlphaCardsRemovedFromSource;
        public int OmegaCardsAddedToHand;
        public int OmegaCardsRemovedFromSource;
    }
}
