using System;

namespace SG03
{
    /// <summary>
    /// Serializable model for a single card definition returned by the
    /// <c>get_card_definitions</c> Lua script.
    /// Field names match the server JSON keys exactly for use with <c>JsonUtility</c>.
    /// </summary>
    [Serializable]
    public class CardDefinitionData
    {
        public string id;
        public string item_code;
        public string name;
        public string description;
        public string category;
        public string rarity;
        public int grid_width;
        public int grid_height;
        public bool is_stackable;
        public bool allow_client_update_qty;
        public bool client_writable;
        public string game_id;
        public string created_at;
        public string created_by;
        public string updated_at;
        public string updated_by;
        public CardDefinitionBaseStats base_stats;
        public CardDefinitionMetadata metadata;
    }
}
