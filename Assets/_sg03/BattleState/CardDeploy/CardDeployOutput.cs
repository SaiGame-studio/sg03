using System;

namespace SG03.UI
{
    /// <summary>
    /// Output payload returned by the card_deploy script.
    /// </summary>
    [Serializable]
    public class CardDeployOutput
    {
        public int    alpha_hand_remaining;
        public string next_move;
        public string session_id;
    }
}
