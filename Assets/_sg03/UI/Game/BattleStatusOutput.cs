using System;

namespace SG03.UI
{
    [Serializable]
    public class BattleStatusOutput
    {
        public int    turn;
        public int    action;
        public int    alpha_hp;
        public int    omega_hp;
        public BattleCardSlot[] alpha_the_source;
        public BattleCardSlot[] alpha_hand;
        public BattleCardSlot[] alpha_back_line;
        public BattleCardSlot[] alpha_front_line;
        public int                 alpha_the_source_count;
        public int                 omega_the_source_count;
        public int                 alpha_the_void_count;
        public int                 omega_the_void_count;
        public BattleCardSlot[] omega_hand;
        public int             omega_hand_count;
        public BattleCardSlot[] omega_front_line;
        public BattleCardSlot[] omega_back_line;
        public string              next_move;
        public string[]            client_actions;
    }
}