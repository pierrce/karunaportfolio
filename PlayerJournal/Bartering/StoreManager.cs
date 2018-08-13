using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StoreManager : NetworkInteractable
{
    [Tooltip("The store number, this should match the database entry.")]
    public int storeNumber;

    [Tooltip("The slots that are in the store.")]
    public List<ShopSlot> shopSlots;

    [Tooltip("The slots that are in the store for selling.")]
    public List<ShopCounterSlot> counterSlots;

    /* The inventory that this store should have when it's reset (server only) */
    private StoreInventory originalStoreInventory;
    /* The inventory for this store (server and client) */
    private StoreInventory currentStoreInventory;

    /* The reset timer for the store. (server only) */
    private float storeResetTimer;

    private const int STORE_RESET_TIME = 15 * 60;

    /* Whether or not to print debug messages for this class */
    private bool debug = true;

    private void Awake()
    {
        currentStoreInventory = new StoreInventory();
        originalStoreInventory = new StoreInventory();
        shopSlots = new List<ShopSlot>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (debug) Debug.Log("Store Manager: Loading information for store number: " + storeNumber);
        StoreInventory storeInventory = SojournDBM.odbm.GetStoreInventory(storeNumber);
        if (storeInventory == null)
            throw new System.Exception("There is no database entry for a store with this number: " + storeNumber);

        /* Keep an original version of the store inventory */
        originalStoreInventory.Apply(storeInventory);
        /* This is the inventory that we will actually use */
        currentStoreInventory.Apply(new StoreInventory(originalStoreInventory));

        /* Reset the store reset timer */
        storeResetTimer = STORE_RESET_TIME;

        PopulateShopSlots();
    }

    private void PopulateShopSlots()
    {
        Item curItem = currentStoreInventory.GetItem(0);
        int curItemNumber = 0;

        if (currentStoreInventory.GetSize() > shopSlots.Count)
        {
            for (int j = 0; j < shopSlots.Count; j++)
            {
                for (int i = 0; i < currentStoreInventory.GetSize(); i++)
                {
                    if (!Item.IsNull(currentStoreInventory.GetItem(i)) && curItem.item_value > currentStoreInventory.GetItem(i).item_value)
                    {
                        curItem = currentStoreInventory.GetItem(i);
                        curItemNumber = i;
                    }
                }
                currentStoreInventory.RemoveItem(currentStoreInventory.GetItem(curItemNumber), 1);
                shopSlots[j].DisplayItem(curItem.item_prefab);
                curItem = currentStoreInventory.GetItem(0);
            }
        }
    }

    /**
     * Returns the store's current inventory.
     */
    public StoreInventory GetCurrentInventory()
    {
        return currentStoreInventory;
    }

    /**
     * Adds an item to the store's inventory. Returns true
     * on success.
     */
    [Server]
    public bool AddItem(Item item)
    {
        if (debug) Debug.Log("StoreManager Server: Adding item to store: " + item);

        if (!currentStoreInventory.AddItem(item, 1))
            return false;

        /* Let the clients know that our inventory has been updated */
        UpdateClientStoreInventory();

        return true;
    }

    /**
     * Removes an item from the store's current inventory. Returns
     * true on success.
     */
    [Server]
    public bool RemoveItem(Item item)
    {
        if (debug) Debug.Log("StoreManager Server: Removing item from store: " + item);

        if (!currentStoreInventory.RemoveItem(item, 1))
            return false;

        /* Let the clients know that our inventory has been updated */
        UpdateClientStoreInventory();

        return true;
    }

    /**
     * Get the current amount of coins in the store.
     */
    public long GetCoins()
    {
        return currentStoreInventory.coins;
    }

    public void AddCoins(long coins)
    {
        this.currentStoreInventory.coins += coins;
    }

    public void RemoveCoins(long coins)
    {
        this.currentStoreInventory.coins -= coins;
    }

    public void SetCoins(long coins)
    {
        this.currentStoreInventory.coins = coins;
    }

    [Server]
    public void UpdateClientStoreInventory()
    {
        if (debug) Debug.Log("StoreManager Server: Sending update for store manager: " + storeNumber);

        currentStoreInventory.Verify();
        RpcUpdateClientStoreInventory(currentStoreInventory);
    }

    [ClientRpc]
    internal void RpcUpdateClientStoreInventory(StoreInventory currentInventory)
    {
        if (debug) Debug.Log("StoreManager Client: We got an update for store " + storeNumber);

        this.currentStoreInventory.Apply(currentInventory);

        /* Get the main player */
        PlayerEntityController player = SojournGameManager.GetMainPlayer();
        Debug.Assert(player != null);

        /* Get the bartering menu manager */
        PlayerBarteringManager menu = player.GetComponent<PlayerBarteringManager>();

        /* Let the manager know that the inventory changed. */
        menu.StoreInventoryChanged(this);
    }

    /**
     * Send an update to a client.
     */
    [Server]
    public void SendUpdate(NetworkConnection conn)
    {
        TargetReceiveUpdate(conn, currentStoreInventory);
    }

    [TargetRpc]
    internal void TargetReceiveUpdate(NetworkConnection conn, StoreInventory inventory)
    {
        if (debug) Debug.Log("StoreManager Client: We received a single update for store " + storeNumber);
        this.currentStoreInventory.Apply(inventory);

        /* Get the main player */
        PlayerEntityController player = SojournGameManager.GetMainPlayer();
        Debug.Assert(player != null);

        /* Get the bartering menu manager */
        PlayerBarteringManager menu = player.GetComponent<PlayerBarteringManager>();

        /* Let the manager know that the inventory changed. */
        menu.StoreInventoryChanged(this);
    }

    public override bool IsCompatibleWithAction(VRInteraction.InteractionEvent action)
    {
        if (VRInteraction.ActionToType(action) == VRInteraction.InteractionType.Trigger)
            return true;
        return false;
    }

    public override bool IsCompatibleWithManager(Component component)
    {
        //if (component is MenuPopupManager)
        //    return true;
        return false;
    }

    [ServerCallback]
    private void Update()
    {
        storeResetTimer -= Time.deltaTime;

        if(storeResetTimer <= 0)
        {
            /* Reset the store inventory */
            currentStoreInventory.Apply(originalStoreInventory);

            /* Reset the timer */
            storeResetTimer = STORE_RESET_TIME;

            if (debug) Debug.Log("StoreManager Server: This store has refresh its inventory: " + storeNumber);

            /* Let the clients know that our inventory has changed. */
            UpdateClientStoreInventory();
        }
    }
}
