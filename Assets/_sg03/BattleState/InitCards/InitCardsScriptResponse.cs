using System;

namespace SG03.UI
{
    /// <summary>
    /// Top-level JSON wrapper for the init_cards script response.
    /// </summary>
    [Serializable]
    public class InitCardsScriptResponse
    {
        public InitCardsOutput output;
    }
}
