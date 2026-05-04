using System;

namespace SG03.UI
{
    /// <summary>
    /// Top-level JSON wrapper for the card_deploy script response.
    /// </summary>
    [Serializable]
    public class CardDeployScriptResponse
    {
        public CardDeployOutput output;
    }
}
