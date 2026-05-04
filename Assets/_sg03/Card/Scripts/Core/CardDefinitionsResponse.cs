using System;

namespace SG03
{
    /// <summary>
    /// Top-level serializable model for the full JSON response of the
    /// <c>get_card_definitions</c> Lua script.
    /// </summary>
    [Serializable]
    public class CardDefinitionsResponse
    {
        public string script_id;
        public string script_name;
        public int version;
        public CardDefinitionsOutput output;
        public int duration_ms;
    }
}
