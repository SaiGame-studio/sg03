using System;

namespace SG03
{
    /// <summary>
    /// Parsed representation of the <c>base_stats</c> JSON field on an
    /// <c>ItemDefinitionData</c>. Field names match the server JSON keys.
    /// Parse via <c>JsonUtility.FromJson&lt;CardBaseStats&gt;(base_stats)</c>.
    /// </summary>
    [Serializable]
    public class CardBaseStats
    {
        public int atk;
        public int def;
        public int star;
    }
}
