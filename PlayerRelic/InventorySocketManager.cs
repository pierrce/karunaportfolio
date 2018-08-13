using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InventorySocketManager : SojournItemSocketManager
{
    /* The inventory we're displaying */
    private PlayerInventory inventory;

    /* Whether or not we are debugging the inventory slot manger */
    private static readonly bool debug = false;

    /**
     * Sets the displayed inventory
     */
    public void SetPlayerInventory(PlayerInventory inventory)
    {
        this.inventory = inventory;
    }

    /**
     * Update the representation of the inventory.
     */
    public void InventoryUpdated()
    {
        if (gridRows != PlayerInventory.INVENTORY_ROWS)
            throw new System.Exception("Slot rows is not equal to inventory rows!");
        if (gridCols != PlayerInventory.INVENTORY_COLS)
            throw new System.Exception("Slot cols is not equal to inventory cols!");
        if (inventory == null)
            return;
        if (!hasGrid)
            throw new System.Exception("Why is this socket manager not in grid mode?");

        if (debug) Debug.Log("InventorySocketManager: Received inventory update.");

        /* Populate the grid */
        for (int row = 0; row < PlayerInventory.INVENTORY_ROWS; row++)
        {
            for (int col = 0; col < PlayerInventory.INVENTORY_COLS; col++)
            {
                /* Get the item at the inventory row,col */
                Item item = inventory.Get(row, col);

                /* Get the slot this item should be placed in */
                SojournItemSocket socket = GetGridItemSocket(row, col);
                Debug.Assert(socket != null);

                /* If there is no longer an item here, destroy the item. */
                if (Item.IsNull(item))
                {
                    /* Do we need to destroy this item? */
                    if(socket.IsOccupied())
                    {
                        if(debug)
                            Debug.Log("InventorySocketManager: (" + row + ", " + col + ") Item will be destroyed.");

                        /* Destroy the item if we need to */
                        if (!socket.ServerDestroyAttachedItem())
                            Debug.LogWarning("InventorySocketManager: (" + row + ", " + col + ") Failed to destroy item in socket!");
                    }

                    /* Don't do anything else with this socket */
                    continue;
                }

                if (debug) Debug.Log("InventorySocketManager: Item in inventory: (" + row + ", " + col + "): " + item.item_name + " : " + item.item_id);

                /* Get the item that's in the socket */
                SojournItem sojournItem = socket.GetAttachedItem();

                /* Is this socket already occupied? */
                if (sojournItem != null)
                {
                    if (debug) Debug.Log("InventorySocketManager: Item in socket: (" + row + ", " + col + "): " + sojournItem.GetItem().item_name 
                        + " : " + sojournItem.GetItem().item_id);

                    /* Are we already displaying this item? */
                    if (sojournItem.GetItem().item_id == item.item_id)
                    {
                        if (debug) Debug.Log("InventorySocketManager: (" + row + ", " + col + ") Requires no change.");
                        continue;
                    }

                    if (debug)
                        Debug.Log("InventorySocketManager: (" + row + ", " + col + ") Destroying attached item.");

                    /* We need to display a different item */
                    if (!socket.ServerDestroyAttachedItem())
                        Debug.LogWarning("InventorySocketManager: (" + row + ", " + col + ") Failed to destroy item in socket!");
                }

                /* Spawn the new item in the socket */
                if (!socket.CreateLocalItemInSocket(item))
                {
                    Debug.LogError("InventorySocketManager: Failed to spawn local only object in inventory: " + item.item_name);
                    return;
                }

                /* Transform the new item */
                sojournItem = socket.GetAttachedItem();
                if (!sojournItem.PlacedInInventory())
                {
                    Debug.LogError("InventorySocketManager: (" + row + ", " + col + ") Failed to scale for inventory!");
                    return;
                }

                if (debug) Debug.Log("InventorySocketManager: Displaying new item at " 
                    + row + "," + col + " item: " + sojournItem.name);
            }
        }
    }
}
