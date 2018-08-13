using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerRelicManager : MenuManager
{
    /* The relic that we're managing (Client or Server) */
    private PlayerRelic relic;

    /* The network instance id of our relic */
    [SyncVar(hook = "RelicUpdated")]
    private NetworkInstanceId relicId = NetworkInstanceId.Invalid;

    /* The toolbelt manager for the player */
    private PlayerToolbeltManager toolbeltManager;
    /* The inventory manager for our player */
    private PlayerInventoryManager playerInventory;
    /* The bank manager for our player */
    private PlayerBankManager playerBank;

    /* The inventory menu object */
    private GameObject inventoryMenuObj;
    /* The bank menu object */
    private GameObject bankMenuObj;

    /* The socket manager for the player's inventory */
    private InventorySocketManager inventoryManager;
    /* The socket manager for the player's bank */
    private BankingSocketManager bankManager;

    /* The height the relic is from the ground when opened */
    private readonly float relicHeight = 1.0f;

    /* The maximum distance the relic cam be from the player  */
    private static readonly float relic_max_distance = 3.0f;

    private static readonly string inventory_menu_name = "RelicUI";
    private static readonly string bank_menu_name = "BankUI";

    /* Whether to display debug messsages or not */
    private readonly bool debug = false;

    protected override void MenuManagerAwake()
    {
        /* Create the menus that we need */
        AddMenu(inventory_menu_name);
        AddMenu(bank_menu_name);

        /* Get the inventory menu and setup manager */
        inventoryMenuObj = GetMenu(inventory_menu_name);
        inventoryManager = inventoryMenuObj.GetComponent<InventorySocketManager>();
        Debug.Assert(inventoryManager != null);

        /* Get the bank menu and setup manager*/
        bankMenuObj = GetMenu(bank_menu_name);
        bankManager = bankMenuObj.GetComponent<BankingSocketManager>();
        Debug.Assert(bankManager != null);
    }

    protected override void AfterOnStartLocalPlayer()
    {
        /* Get the toolbelt manager from the player */
        toolbeltManager = GetComponent<PlayerToolbeltManager>();
        if (toolbeltManager == null)
            throw new System.Exception("Why does this VR player not have a toolbelt!");

        /* Get the inventory manager for this player */
        playerInventory = GetComponent<PlayerInventoryManager>();
        if (playerInventory == null)
            throw new System.Exception("Why is there no player inventory manager for this player?");

        /* Get the bank manager for this player */
        playerBank = GetComponent<PlayerBankManager>();
        if (playerBank == null)
            throw new System.Exception("Why is there no player bank manager for this player?");

        /* Get the relic menu object */
        RelicUIManager relicUI = inventoryMenuObj.GetComponent<RelicUIManager>();
        Debug.Assert(relicUI != null);
        relicUI.SetPlayerInventory(playerInventory.GetInventory());

        /* Get the bank menu object */
        BankUIManager bankUI = bankMenuObj.GetComponent<BankUIManager>();
        Debug.Assert(bankUI != null);
        bankUI.SetPlayerBankManager(playerBank);
    }

    public override void OnStartClient()
    {
        /* Manually call our hook if we need to */
        if (relicId != NetworkInstanceId.Invalid)
            RelicUpdated(relicId);
    }

    [Client]
    private void RelicUpdated(NetworkInstanceId relicId)
    {
        /* Make sure the id isn't invalid */
        if (relicId == NetworkInstanceId.Invalid)
            return;

        /* Save the relic id */
        this.relicId = relicId;

        /* Get the relic object */
        GameObject relicObj = ClientScene.FindLocalObject(relicId);
        if(relicObj == null)
        {
            Debug.LogError("PlayerRelicManager Client: Server sent us a bad network instance id for our relic! " + relicId.Value);
            return;
        }

        /* Get the relic component */
        relic = relicObj.GetComponent<PlayerRelic>();
        if (relic == null)
        {
            Debug.LogError("PlayerRelicManager Client: Server sent us a bad network instance id for our relic! " + relicId.Value);
            return;
        }

        /* We own this relic */
        relic.SetRelicManager(this);

        if (debug) Debug.Log("PlayeRelicManager Client: We have received our player relic object.");
    }

    /**
     * Called when an item is spawned in the player's toolbelt.
     */
    [Server]
    protected override bool OnServerItemSpawnedInToolbelt(GameObject obj, PlayerToolbeltSlot slot)
    {
        /* Do we already have a relic? */
        if (relic != null)
            return false;

        /* Is this our player relic? */
        relic = obj.GetComponent<PlayerRelic>();
        if (relic == null)
            return false;

        if (debug) Debug.Log("PlayerRelicManager Server: Player relic has spawned.");

        /* Set our relic id. This will trigger hooks on clients */
        relicId = relic.netId;

        /* We consumed this event */
        return true;
    }

    /**
     * When the relic is picked up it sets all lerps to false and closes the relic if its open 
     */
    protected override void OnAfterPickedUp(GameObject obj, HandManager hand)
    {
        /* We only care about events from the local player */
        if (!isLocalPlayer)
            return;

        /* Is this our relic? */
        if (relic == null || relic.gameObject != obj)
            return;

        /* Debug mode messages */
        if (debug) Debug.Log("PlayerRelicManager: Relic has been picked up!");

        /* Close the relic */
        relic.ClientClose();
    }

    protected override bool OnItemPlacedInToolbelt(GameObject obj, PlayerToolbeltSlot slot, HandManager hand)
    {
        /* Is this our relic? */
        if (relic == null || relic.gameObject != obj)
            return false;

        /* Debug mode messages */
        if (debug) Debug.Log("PlayerRelicManager: The relic has been placed in the toolbelt!");

        /* Close the relic */
        relic.ClientClose();

        /* We consumed this event */
        return true;
    }

    /**
     * When the relic is dropped, starts the sequence of opening
     * the relic and lerping it to face the player 
     */
    protected override void OnAfterDropped(GameObject obj, HandManager hand)
    {
        if (relic == null || relic.gameObject != obj || !isClient)
            return;

        /* If the relic is attached to something, we can't open. */
        if(relic.IsAttached())
        {
            if (debug) Debug.Log("PlayerRelicManager: The relic is attached to the toolbelt, so it can't open!");
            return;
        }

        /* Set the relic position based off of the height of the player */
        Debug.Assert(CameraRigManager.crm_singleton != null);
        Vector3 relicPosition = new Vector3(relic.transform.position.x,
            CameraRigManager.crm_singleton.transform.position.y + relicHeight,
            relic.transform.position.z);

        /* Open the relic */
        relic.ClientOpenInventory(relicPosition);
    }

    protected override bool OnControllerInteraction(VRInteraction.InteractionEvent action, GameObject interactable, HandManager hand)
    {
        /* Was this action a trigger press? */
        if (action != VRInteraction.InteractionEvent.OnTriggerPressed)
            return false;

        /* Get the money manager from the interactable */
        RelicCoinBag coinBag = interactable.GetComponent<RelicCoinBag>();
        if (coinBag == null)
            return false;

        ///* Let the money manager know they received a trigger click */
        //RelicWithdrawInterface withdraw = coinBag.GetComponentInParent<RelicWithdrawInterface>();
        //Debug.Assert(withdraw != null);

        ///* Let the withdraw interface know there was a click on the coin bag */
        //withdraw.OnTriggerClick(hand, playerInventory.GetInventory());

        /* We used this event */
        return true;
    }

    /**
     * Called by the player relic when the relic finishes openening.
     */
    public void RelicOpened(PlayerRelic.RelicConfiguration config)
    {
        if (!isLocalPlayer)
            return;

        /* Update the player's menu */
        RelicUIManager playerUI = inventoryMenuObj.GetComponent<RelicUIManager>();
        Debug.Assert(playerUI != null);
        playerUI.SetPlayerInventory(playerInventory.GetInventory());
        playerUI.InventoryUpdated();

        /* Update the player's bank menu */
        BankUIManager bankUI = bankMenuObj.GetComponent<BankUIManager>();
        Debug.Assert(bankUI != null);
        bankUI.SetPlayerBankManager(playerBank);
        bankUI.BankUpdated();

        switch (config)
        {
            case PlayerRelic.RelicConfiguration.OpenInventory:
                /* Reposition the player inventory */
                inventoryMenuObj.transform.parent = relic.gameObject.transform;
                inventoryMenuObj.transform.localPosition = Vector3.zero;
                inventoryMenuObj.transform.localRotation = Quaternion.identity;

                /* Show the inventory menu */
                ShowMenu(inventory_menu_name);
                break;
            case PlayerRelic.RelicConfiguration.OpenBank:
                /* Reposition the player inventory */
                inventoryMenuObj.transform.parent = relic.bankUIRelicPosition.transform;
                inventoryMenuObj.transform.localPosition = Vector3.zero;
                inventoryMenuObj.transform.localRotation = Quaternion.identity;

                /* Reposition the bank inventory */
                bankMenuObj.transform.parent = relic.bankUIPosition.transform;
                bankMenuObj.transform.localPosition = Vector3.zero;
                bankMenuObj.transform.localRotation = Quaternion.identity;

                /* Show menues */
                ShowMenu(inventory_menu_name);
                ShowMenu(bank_menu_name);
                break;
        }
    }

    [Client]
    protected override bool OnItemDroppedOnInteractionBox(InteractionBox box, GameObject obj, HandManager hand)
    {
        if (relic == null)
            return false;
        if (obj != relic.gameObject)
            return false;

        if (!(box is BankAlter))
            return false;

        if (debug) Debug.Log("PlayerRelicManager Client: Player relic dropped onto a bank alter.");

        /* Get the bank alter */
        BankAlter alter = (BankAlter)box;

        /* Is this alter occupied? */
        if(alter.IsOccupied())
        {
            if (debug) Debug.LogWarning("PlayerRelicManager Client: This alter is occupied!");
            return false;
        }

        /* Attempt to attach our relic to the bank alter */
        if(!relic.ClientReattach(alter))
        {
            Debug.LogError("PlayerRelicManager Client: Failed to attach relic to alter socket!");
            return false;
        }

        /* Open the relic in bank config */
        if(!relic.ClientOpenBank())
        {
            Debug.LogError("PlayerRelicManager Client: Failed to open relic in bank configuration!");
            return false;
        }

        /* We consumed this event */
        return true;
    }

    /**
     * Called by the player relic when the relic starts closing.
     */
    public void RelicClosing()
    {
        /* Just hide all of the menus */
        HideMenus();
    }

    protected override void OnInventoryUpdated()
    {
        /* We only care if this is the local player */
        if (!isLocalPlayer)
            return;

        /* Do we have an inventory slot manager? */
        if (inventoryManager != null)
        {
            if (debug) Debug.Log("PlayerRelicManager Client: Populating inventory slot manager...");

            /* Let the slot manager know that it should be repopulated */
            inventoryManager.InventoryUpdated();
        }
    }

    protected override void OnBankUpdated()
    {
        /* We only care if this is the local player */
        if (!isLocalPlayer)
            return;

        /* Do we have an inventory slot manager? */
        if (bankManager != null)
        {
            if (debug) Debug.Log("PlayerRelicManager Client: Populating bank slot manager...");

            /* Let the slot manager know that it should be repopulated */
            bankManager.BankUpdated();
        }
    }

    protected override void _Update()
    {
        if (!isLocalPlayer || relic == null)
            return;

        /* Should we close the relic? */
        if (relic.IsOpen() && Vector3.Distance(transform.position, relic.transform.position) > relic_max_distance)
        {
            /* Close the relic */
            relic.ClientClose();

            /* Return the relic to the toolbelt */
            toolbeltManager.ReturnItem(relic);
        }
    }
}
