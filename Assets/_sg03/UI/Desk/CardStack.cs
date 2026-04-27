using System.Collections.Generic;
using SaiGame.Services;

namespace SG03.UI
{
    // Groups inventory items that share the same item_definition_id into one display unit.
    // ItemIds holds every individual instance id available to add to a desk slot.
    internal sealed class CardStack
    {
        public readonly InventoryItemData Representative;
        public readonly List<string> ItemIds;
        public int Count => this.ItemIds.Count;

        public CardStack(InventoryItemData representative)
        {
            this.Representative = representative;
            this.ItemIds        = new List<string> { representative.id };
        }

        public void Add(string itemId)
        {
            this.ItemIds.Add(itemId);
        }
    }
}
