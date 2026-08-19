using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SG03
{
    /// <summary>
    /// Serializable model for a single card definition returned by the
    /// <c>get_card_definitions</c> Lua script.
    /// Field names match the server JSON keys exactly. <c>base_stats</c> is a
    /// dynamic map, so new server-side stats do not require client model changes.
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
        public Dictionary<string, JToken> base_stats;
        public CardDefinitionMetadata metadata;

        public bool TryGetBaseStat(string key, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(key) || base_stats == null) return false;

            JToken token;
            if (!base_stats.TryGetValue(key, out token) || token == null || token.Type == JTokenType.Null)
                return false;

            value = token.Type == JTokenType.String
                ? token.Value<string>()
                : token.ToString(Formatting.None);
            return true;
        }

        public int GetBaseStatInt(string key)
        {
            string value;
            int result;
            return TryGetBaseStat(key, out value)
                   && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result)
                ? result
                : 0;
        }
    }
}
