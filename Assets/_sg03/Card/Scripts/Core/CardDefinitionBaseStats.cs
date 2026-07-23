using System;

namespace SG03
{
    /// <summary>
    /// All possible numeric stat fields returned by the server in a card definition's
    /// <c>base_stats</c> object. Missing fields default to 0.
    /// Field names match the server JSON keys exactly for use with <c>JsonUtility</c>.
    /// </summary>
    [Serializable]
    public class CardDefinitionBaseStats
    {
        public int atk;
        public int def;
        public int star;
        public int atk_add;
        public int atk_deduct;
        public int def_add;
        public int def_deduct;
        public int summon_count;
        public int hp_restore;
    }
}
