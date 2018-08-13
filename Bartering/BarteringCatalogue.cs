using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarteringCatalogue : UICatalogueSlotManager
{
    private StoreInventory storeInventory;

    public void SetStoreInventory(StoreInventory storeInventory)
    {
        this.storeInventory = storeInventory;
    }

    public StoreInventory GetStoreInventory()
    {
        return storeInventory;
    }

    public override void Populate()
    {
        if (storeInventory == null)
            return;

        /* Remove all items from the catalogue */
        ClearItems();

        for(int x = 0;x < StoreInventory.MAX_ITEMS;x++)
        {
            /* Get the item from the store inventory  */
            Item item = storeInventory.GetItem(x);

            /* If this item is null, skip it */
            if (item == null)
                continue;

            /* Add the item to the bartering catalogue */
            AddItem(item);
        }
    }
}
