using System;

namespace SG03.UI
{
    [Serializable]
    public class BattleStartPayload
    {
        public string battle_mode;
        public string enemy_entity_key;
        public string preset_instance_id;
    }
}