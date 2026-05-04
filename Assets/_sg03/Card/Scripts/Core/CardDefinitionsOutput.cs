using System;

namespace SG03
{
    /// <summary>
    /// Serializable model for the <c>output</c> block of a
    /// <c>get_card_definitions</c> script response.
    /// </summary>
    [Serializable]
    public class CardDefinitionsOutput
    {
        public string[] codes;
        public CardDefinitionData[] definitions;
        public string session_id;
        public int total;
    }
}
