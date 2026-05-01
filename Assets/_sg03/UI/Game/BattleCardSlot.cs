using System;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>
    /// Represents a single card slot in a battle zone (hand, back line, front line).
    /// item_definition_code_name is the lookup key.
    /// </summary>
    [Serializable]
    public class BattleCardSlot
    {
        [Tooltip("Lookup key")]
        public string item_definition_code_name;
        public string inventory_item_id;
        public string item_definition_id;
        public string item_definition_name;
    }
}
