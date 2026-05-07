using System;
using UnityEngine;

namespace SG03.UI
{
    /// <summary>
    /// Represents a single card slot in the omega hand returned by the init_cards script.
    /// Uses item_code_name as the lookup key (unlike alpha which uses item_definition_code_name).
    /// </summary>
    [Serializable]
    public class OmegaInitCardSlot
    {
        public string id;
        [Tooltip("Lookup key")]
        public string item_code_name;
        public int    slot_index;
    }
}
