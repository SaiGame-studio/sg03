using System;

namespace SG03.UI
{
    /// <summary>
    /// Output payload returned by the init_cards script.
    /// </summary>
    [Serializable]
    public class InitCardsOutput
    {
        public int                alpha_cards_drawn;
        public BattleCardSlot[]   alpha_hand;
        public int                omega_cards_drawn;
        public OmegaInitCardSlot[] omega_hand;
        public string             session_id;
        public int                alpha_the_source_count;
        public int                omega_the_source_count;
    }
}
