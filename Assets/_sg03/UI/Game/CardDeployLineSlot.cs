using System;

namespace SG03.UI
{
    /// <summary>
    /// Represents a single slot entry in the front_line or back_line payload
    /// sent to the card_deploy script.
    /// </summary>
    [Serializable]
    public class CardDeployLineSlot
    {
        public string inventory_item_id;
        public bool   face_up;
    }
}
