using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicUIManager : MonoBehaviour
{
    /* The socket manager that displays the inventory */
    private InventorySocketManager socketManager;

    private void Awake()
    {
        socketManager = GetComponentInChildren<InventorySocketManager>();
        Debug.Assert(socketManager != null, name);
    }

    /**
     * Sets the inventory that this inventory slot manager should display.
     */
    public void SetPlayerInventory(PlayerInventory inventory)
    {
        socketManager.SetPlayerInventory(inventory);
    }

    /**
     * Called when the player's inventory changes.
     */
    public void InventoryUpdated()
    {
        socketManager.InventoryUpdated();
    }
}
