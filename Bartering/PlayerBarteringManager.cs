using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerBarteringManager : MenuManager
{
    /* The catalogue manager (Client only) */
    private BarteringCatalogue barteringCatalogue;
    /* The inventory on the left side of the screen (Client only) */
    private PlayerBarteringManager playerInventoryManager;
    /* The store we are currently interacting with (Client only) */
    private StoreManager storeManager;

    private bool debug = true;

    protected override bool OnInteractionBoxPressed(InteractionBox box, HandManager hand)
    {
        /* Get the item slot from the interaction box */
        UIItemSlot itemSlot = box.GetGameObject().GetComponent<UIItemSlot>();
        if (itemSlot == null)
            return false;

        /* The store manager has to be setup */
        if (storeManager == null)
            return false;

        /* See if either of our slot managers own this slot */
        bool catalogueOwnsSlot = barteringCatalogue.OwnsSlot(itemSlot);

        /* Get the item from the slot */
        SojournItem transfoerSojournItem = itemSlot.GetItem();
        if (transfoerSojournItem == null)
            return true;
        Item transfer = transfoerSojournItem.GetItem();
        if (transfer == null)
            return true;

        if(catalogueOwnsSlot)
        {
            CmdTransferFromStoreToInventory(storeManager.GetComponent<NetworkIdentity>().netId, transfer.item_name);
        } 

        return true;
    }

    public void StoreInventoryChanged(StoreManager manager)
    {
        /* If this is the store we care about, repopulate the UI */
        if (this.storeManager == manager)
            barteringCatalogue.Populate();
    }

    /**
     * Set the store inventory we should be looking at.
     */
    public void SetStoreManager(StoreManager storeManager)
    {
        this.storeManager = storeManager;

        /* Let the bartering catalogue know that the store inventory has changed */
        barteringCatalogue.SetStoreInventory(storeManager.GetCurrentInventory());
    }

    //protected override void OnBeforeMenuShown()
    //{
    //    /* Update the catalogue */
    //    barteringCatalogue.Populate();
    //}

    [Command]
    internal void CmdTransferFromInventoryToStore(NetworkInstanceId store_id, int row, int col)
    {
        if (debug) Debug.Log("BarteringMenuManager Server: Got a request to move an item from the inventory to the store.");

        /* Get the player's attribute manager */
        PlayerEntityController attrib = GetComponent<PlayerEntityController>();

        /* Get this player's inventory */
        PlayerInventory inventory = attrib.GetComponent<PlayerInventoryManager>().GetInventory();

        /* Get the item from the player's inventory */
        Item item = inventory.Get(row, col);
        if (Item.IsNull(item))
        {
            Debug.LogError("BarteringMenuManager Server: Can't take null item from inventory!");
            return;
        }

        GameObject storeObject = NetworkServer.FindLocalObject(store_id);
        if(storeObject == null)
        {
            Debug.LogError("BarteringMenuManager Server: The client send us a bad object id!");
            return;
        }

        /* Get the store manager for this store */
        StoreManager storeManager = storeObject.GetComponent<StoreManager>();

        if(storeManager == null)
        {
            Debug.LogError("BarteringMenuManager Server: The client sent us an ID for something that isn't a store!");
            return;
        }

        /* Take coins from player and give them to the store */
        long value = item.item_value;
        if(storeManager.GetCoins() < value)
        {
            if(debug) Debug.LogWarning("BarteringMenuManager Server: The store doesn't have enough coins to buy this item!");

            /* Reduce the value to the amount of coins remaining in the shop */
            value = storeManager.GetCoins();
        }

        inventory.gold += value;
        storeManager.RemoveCoins(value);

        /* Remove the item from the player's inventory */
        if (inventory.Remove(row, col) == null)
        {
            Debug.LogError("BarteringMenuManager Server: Failed to remove item from player inventory!");
            return;
        }

        /* Give the item to the store */
        if(!storeManager.AddItem(item))
        {
            Debug.LogError("BarteringMenuManager Server: Failed to add item to the shop!!");

            /* If this happens we should really restore the player's item. */
            if (inventory.Add(item))
                Debug.LogWarning("BarteringMenuManager Server: We were able to restore the player's item.");
            else Debug.LogError("BarteringMenuManager Server: We failed to restore the player's item!!");

            return;
        }

        /* The player's inventory has been updated */
        // attrib.UpdateClientInventory();

        if (debug) Debug.Log("BarteringMenuManager Server: Item transferred to the player inventory.");
    }

    [Command]
    internal void CmdTransferFromStoreToInventory(NetworkInstanceId store_id, string item_name)
    {
        /* Get the item that the player is trying to purchase */
        Item item = SojournDBM.odbm.GetItemByName(item_name);
        if(Item.IsNull(item))
        {
            Debug.LogError("BarteringMenuManager Server: Client sent us a bad item uuid.");
            return;
        }

        if (debug) Debug.Log("BarteringMenuManager Server: Got a request to move an item from the inventory to the store.");

        /* Get the player's attribute manager */
        PlayerEntityController attrib = GetComponent<PlayerEntityController>();

        /* Get this player's inventory */
        PlayerInventory inventory = attrib.GetComponent<PlayerInventoryManager>().GetInventory();

        /* Get the object for the store */
        GameObject storeObject = NetworkServer.FindLocalObject(store_id);
        if (storeObject == null)
        {
            Debug.LogError("BarteringMenuManager Server: The client send us a bad object id!");
            return;
        }

        /* Get the store manager for this store */
        StoreManager storeManager = storeObject.GetComponent<StoreManager>();

        if (storeManager == null)
        {
            Debug.LogError("BarteringMenuManager Server: The client sent us an ID for something that isn't a store!");
            return;
        }

        /* Does the store carry the given item? */
        if(storeManager.GetCurrentInventory().GetQuantity(item) <= 0)
        {
            Debug.LogError("BarteringMenuManager Server: The store doesn't carry this item.");
            return;
        }

        /* Take coins from store and give them to the player */
        long value = item.item_value;
        if (inventory.gold < value)
        {
            if (debug) Debug.LogWarning("BarteringMenuManager Server: The player doesn't have enough coins to buy this item!");
            return;
        }

        inventory.gold -= value;
        storeManager.AddCoins(value);

        /* Remove the item from the store */
        if (!storeManager.RemoveItem(item))
        {
            Debug.LogError("BarteringMenuManager Server: Failed to remove the item from the store!");
            return;
        }

        /* Give the item to the player */
        if (!inventory.Add(item))
        {
            Debug.LogError("BarteringMenuManager Server: Failed to add item to the player's inventory!!");

            /* If this happens we should really restore the store's item. */
            if (storeManager.AddItem(item))
                Debug.LogWarning("BarteringMenuManager Server: We were able to restore the store's item.");
            else Debug.LogError("BarteringMenuManager Server: We failed to restore the store's item!!");

            return;
        }

        /* The player's inventory has been updated */
        // attrib.UpdateClientInventory();

        if (debug) Debug.Log("BarteringMenuManager Server: Item transferred to the inventory.");
    }

    [Client]
    public void RequestStoreUpdate(StoreManager store)
    {
        CmdRequestStoreRefresh(store.GetComponent<NetworkIdentity>().netId);
    }

    [Command]
    internal void CmdRequestStoreRefresh(NetworkInstanceId storeId)
    {
        /* Get the store object */
        GameObject storeObject = NetworkServer.FindLocalObject(storeId);
        if(storeObject == null)
        {
            Debug.LogError("BarteringMenuManager Server: Client sent us a bad store id.");
            return;
        }

        /* Get the store manager from the store object */
        StoreManager store = storeObject.GetComponent<StoreManager>();
        if (store == null)
        {
            Debug.LogError("BarteringMenuManager Server: Client sent us an object that isn't a store manager!");
            return;
        }

        /* Send the store update */
        store.SendUpdate(connectionToClient);
    }
}
