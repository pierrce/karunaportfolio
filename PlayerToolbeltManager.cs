using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerToolbeltManager : InteractionManager
{
    [Tooltip("The prefab for the player's toolbelt")]
    public GameObject toolbeltPrefab;

    /* The toolbelt component for this player (Client and server) */
    private PlayerToolbelt toolbelt;
    /* The network instance id of our toolbelt */
    [SyncVar(hook = "ToolbeltUpdated")]
    private NetworkInstanceId toolbeltId = NetworkInstanceId.Invalid;

    /* Whether or not to print debug for this class */
    private static readonly bool debug = false;

    protected override void AfterAwake()
    {
        /* We require a toolbelt prefab */
        if (toolbeltPrefab == null)
            throw new System.Exception("You need to set a toolbelt prefab!");
    }

    protected override void OnClientReady()
    {
        /* Spawn the player's toolbelt */
        SpawnToolbelt();
    }

    public override void OnPlayerDisconnect(string id)
    {
        if (debug)
            Debug.Log("PlayerToolbeltManager: Player has disconnected.");
        /* Destroy items attached to the toolbelt */
        toolbelt.ServerDestroyItems();

        /* Destroy the toolbelt */
        NetworkServer.Destroy(toolbelt.gameObject);
    }

    public override void OnStartClient()
    {
        /* Manually call hook */
        if (toolbeltId != NetworkInstanceId.Invalid)
        {
            if (debug) Debug.Log("PlayerToolbeltManager Client: We had to manually call the hook.");
            ToolbeltUpdated(toolbeltId);
        }
    }

    public override void WriteToDB(string id)
    {
        /* If we don't have a toolbelt there is nothing to update */
        if (toolbelt == null)
            return;

        /* Get the items from the toolbelt */
        Item[] toolbeltItems = toolbelt.GetCustomItems();
        /* Update the toolbelt items */
        if (SojournDBM.odbm.UpdatePlayerToolbeltItems(id, toolbeltItems))
            Debug.LogError("PlayerToolbeltManager: Failed to update player toolbelt items.");

        if (debug) Debug.Log("PlayerToolbeltManager: Updated player toolbelt items in database.");
    }

    private void ToolbeltUpdated(NetworkInstanceId toolbeltId)
    {
        /* Don't use hooks on host only clients */
        if (isServer && isClient)
            return;

        if (debug)
            Debug.Log("PlayerToolbeltManager Client: Our toolbelt ID has been updated ");

        /* Get the toolbelt object */
        GameObject obj = ClientScene.FindLocalObject(toolbeltId);
        Debug.Assert(obj != null);

        /* Get the toolbelt from the object */
        toolbelt = obj.GetComponent<PlayerToolbelt>();
        Debug.Assert(toolbelt != null);

        if (debug) Debug.Log("PlayerToolbeltManager Client: Our toolbelt has spawned!");

        /* Do client callbacks */
        eventManager.OnToolbeltEvent(VRInteraction.InteractionEvent.OnToolbeltSpawned, toolbelt.gameObject, null, null);
    }

    [Server]
    private void SpawnToolbelt()
    {
        /* Do we already have a toolbelt? */
        if(toolbelt != null)
        {
            Debug.LogError("PlayerToolbeltManager: We already have a toolbelt!");
            return;
        }

        if (debug) Debug.Log("PlayerToolbeltManager: We are going to spawn this player's toolbelt.");

        /* Spawn the toolbelt */
        GameObject toolbeltObj = Instantiate(toolbeltPrefab);
        if (toolbeltObj == null)
            throw new System.Exception("Failed to create toolbelt!");
        toolbeltObj.transform.position = transform.position;

        /* Get the toolbelt component from the object */
        toolbelt = toolbeltObj.GetComponent<PlayerToolbelt>();
        if (toolbelt == null)
            throw new System.Exception("Why does this toolbelt object not have a toolbelt component?");

        /* Set the id of the player */
        toolbelt.SetPlayer(playerController);

        /* Spawn our toolbelt with our authority */
        if (!NetworkServer.SpawnWithClientAuthority(toolbeltObj, gameObject))
            throw new System.Exception("Failed to spawn toolbelt!");

        if (debug) Debug.Log("PlayerToolbeltManager Server: Spawned player's toolbelt: " + playerController.GetEntityName());

        /* Do our toolbelt spawned callback */
        eventManager.OnToolbeltEvent(VRInteraction.InteractionEvent.OnToolbeltSpawned, toolbeltObj, null, null);

        /* Get the custom items for this toolbelt from the database */
        Item[] customToolbeltItems = SojournDBM.odbm.GetPlayerToolbeltItems(playerController.GetEntityId());
        if(customToolbeltItems == null)
            Debug.LogError("PlayerToolbeltManager: Failed to load toolbelt items for player: " + playerController.GetEntityName());

        /* Print custom items in debug mode */
        if(debug)
        {
            foreach(Item item in customToolbeltItems)
            {
                if (item == null)
                    continue;
                Debug.Log("PlayerToolbeltManager: Custom item: " + item.item_name);
            }
        }

        /* Spawn the items in the toolbelt */
        toolbelt.SpawnToolbeltItems(customToolbeltItems);

        /* Save our toolbelt id */
        toolbeltId = toolbelt.netId;
    }

    [Client]
    protected override bool OnControllerInteraction(VRInteraction.InteractionEvent action, GameObject interactable, HandManager hand)
    {
        /* Make sure our toolbelt is actually setup */
        if (toolbelt == null)
            return false;

        /* Is the interactable a toolbelt slot? */
        PlayerToolbeltSlot slot = interactable.GetComponent<PlayerToolbeltSlot>();
        if(slot == null || !slot.GetSocketManager().hasAuthority)
            return false; 

        /* This has to be a pickup action */
        if (!SojournItem.IsPickupEvent(action))
            return false;

        if (debug) Debug.Log("PlayerToolbeltManager Client: The player is attempting to pickup something from our toolbelt slot.");

        /* The slot must be occupied */
        if(!slot.IsOccupied())
        {
            if (debug) Debug.LogError("PlayerToolbeltManager Client: This toolbelt slot isn't occupied!");
            return false;
        }

        /* Get the object that's in the slot we're interacting with */
        GameObject toolbeltItemObj = slot.GetAttachedObject();
        if (toolbeltItemObj == null)
            throw new System.Exception("Why is there no attached item if this slot is occupied?");

        /* Convert the object to an item */
        SojournItem toolbeltItem = toolbeltItemObj.GetComponent<SojournItem>();
        if (toolbeltItem == null)
            throw new System.Exception("Why is this object attached to the toolbelt but it's not an item!!");

        /* If the hand isn't empty, swap the items */
        if(hand.IsHoldingSomething())
        {
            if (debug) Debug.LogWarning("PlayerToolbeltManager Client: The player is already holding something so we'll swap items");

            /* We have to do the swap on the server */
            //CmdSwapItems(hand.netId, slot.GetSocketManager().netId, slot.socketNumber);

            /* We swapped items */
            return true;
        }

        if (debug) Debug.Log("PlayerToolbeltManager Client: We weren't holding something, so we're just picking up the object");
        
        /* Pickup the item from the toolbelt */
        CmdPickupItem(hand.netId, slot.GetSocketManager().netId, slot.GetSocketNumber());

        /* We picked up the object */
        return true;
    }

    [Command]
    private void CmdPickupItem(NetworkInstanceId handId, NetworkInstanceId socketManagerId, int socketNumber)
    {
        if (debug) Debug.Log("PlayerToolbeltManager Server: The client has asked us to swap items between their hand and their toolbelt");

        /* Get our hand manager */
        HandManager hand = InternalGetHand(handId);
        if (hand == null)
            return;

        /* Get the socket that the client provided */
        SojournSocket socket = GetSojournSocket(socketManagerId, socketNumber);
        if (socket == null)
            return;

        /* Convert the socket into a toolbelt slot */
        PlayerToolbeltSlot toolbeltSlot = (PlayerToolbeltSlot)socket;
        if (toolbeltSlot == null)
        {
            Debug.LogError("PlayerToolbeltManager Server: The client sent us a bad toolbelt slot!");
            return;
        }

        /* Get the attached object from the toolbelt slot */
        GameObject attachedObj = toolbeltSlot.GetAttachedObject();
        if (attachedObj == null)
        {
            Debug.LogError("SojournToolbeltManager Server: There is nothing attached to this toolbelt slot!");
            return;
        }

        /* Get the toolbelt item from the socket */
        SojournItem toolbeltItem = attachedObj.GetComponent<SojournItem>();
        if (toolbeltItem == null)
            return;

        /* Make the toolbelt item pickupable */
        toolbeltItem.SetCanBePickedUp(true);

        /* Get the player controller for this player */
        PlayerEntityController player = hand.GetPlayerController();
        if (player == null)
            throw new System.Exception("Why is there no player controller for this hand?");

        /* Get the player pickup manager for this hand */
        PlayerPickupManager pickup = player.GetComponent<PlayerPickupManager>();
        if (!pickup.ServerPickUp(hand, toolbeltItem))
            throw new System.Exception("Failed to have player pickup item on toolbelt!");

        if (debug) Debug.Log("PlayerToolbeltManager Server: Player picked up item from their toolbelt.");
    }

    [Command]
    private void CmdSwapItems(NetworkInstanceId handId, NetworkInstanceId socketManagerId, int socketNumber)
    {
        if (debug) Debug.Log("PlayerToolbeltManager Server: The client has asked us to swap items between their hand and their toolbelt");

        /* Get our hand manager */
        HandManager hand = InternalGetHand(handId);
        if (hand == null)
            return;

        /* Get the socket that the client provided */
        SojournSocket socket = GetSojournSocket(socketManagerId, socketNumber);
        if (socket == null)
            return;

        /* Conver the socket into a toolbelt slot */
        PlayerToolbeltSlot toolbeltSlot = (PlayerToolbeltSlot)socket;
        if (toolbeltSlot == null)
        {
            Debug.LogError("PlayerToolbeltManager Server: The client sent us a bad toolbelt slot!");
            return;
        }

        /* Get the attached object from the toolbelt slot */
        GameObject attachedObj = toolbeltSlot.GetAttachedObject();
        if (attachedObj == null)
        {
            Debug.LogError("SojournToolbeltManager Server: There is nothing attached to this toolbelt slot!");
            return;
        }

        /* Get the toolbelt item from the socket */
        SojournItem toolbeltItem = attachedObj.GetComponent<SojournItem>();
        if (toolbeltItem == null)
            return;

        /* Make the toolbelt item pickupable */
        toolbeltItem.SetCanBePickedUp(true);

        if (debug) Debug.Log("PlayerToolbeltManager Server: We are allowing the swap");

        /* Get the object from the player's hand */
        GameObject handItemObj = hand.GetPickedUpObject();
        if (handItemObj == null)
            throw new System.Exception("There is supposed to be something in our hand!");

        /* Get the sojourn item component from the hand object */
        SojournItem handItem = handItemObj.GetComponent<SojournItem>();
        if (handItem == null)
            throw new System.Exception("Why is this player holding something that isn't a sojourn item!");

        /* Detach the item from the toolbar socket */
        if (!toolbeltItem.ServerDetach())
            throw new System.Exception("Failed to detach item from toolbelt!");

        /* Attach The item to the toolbar */
        if (!handItem.ServerAttach(toolbeltSlot, playerController))
            throw new System.Exception("Failed to attach hand item to toolbelt!");

        PlayerPickupManager pickup = hand.GetPlayerController().GetComponent<PlayerPickupManager>();
        Debug.Assert(pickup != null);

        /* Tell the player to pickup the item on their toolbelt */
        if (!pickup.ClientPickUp(hand, toolbeltItem))
            throw new System.Exception("Failed to pickup item that was attached to our toolbelt!");

        if (debug) Debug.Log("PlayerToolbeltManager Server: We have swapped the items");
    }

    private SojournSocket GetSojournSocket(NetworkInstanceId socketManagerId, int socketNumber)
    {
        /* Get the socket manager object that the client sent us */
        GameObject socketManagerObj = NetworkServer.FindLocalObject(socketManagerId);
        if (socketManagerObj == null)
        {
            Debug.LogError("PlayerToolbeltManager Server: Client has sent us a bad socket manager object!");
            return null;
        }

        /* Get the socket manager component from the object */
        PlayerToolbelt playerToolbelt = socketManagerObj.GetComponent<PlayerToolbelt>();
        if (playerToolbelt == null)
        {
            Debug.LogError("PlayerToolbeltManager Server: Client sent us an object that isn't an item socket manager!");
            return null;
        }

        /* Make sure the client owns this socket manager */
        if (playerToolbelt.GetPlayerId() != netId)
        {
            Debug.LogError("PlayerToolbeltManager Server: Client doesn't own this socket manager!");
            return null;
        }

        /* Get the item socket from the socket manager */
        SojournSocket itemSlot = playerToolbelt.GetSocket(socketNumber);
        if (itemSlot == null)
        {
            Debug.LogError("PlayerToolbeltManager Server: This socket number is invalid: " + socketNumber);
            return null;
        }

        /* Return the toolbelt item */
        return itemSlot;
    }

    [Server]
    private HandManager InternalGetHand(NetworkInstanceId handId)
    {
        /* Get the hand object */
        GameObject handObj = NetworkServer.FindLocalObject(handId);
        if (handObj == null)
        {
            Debug.LogError("PlayerToolbeltManager Server: Client sent us a bad hand id!");
            return null;
        }

        /* Get the hand component from the object */
        HandManager hand = handObj.GetComponent<HandManager>();
        if (hand == null)
        {
            Debug.LogError("PlayerToolbeltManager Server: Client sent us something that isn't a hand!");
            return null;
        }

        /* The player has to own this hand */
        if (hand.GetPlayerController().connectionToClient != connectionToClient)
        {
            Debug.LogError("PlayerToolbeltManager Server: Client sent us a hand that they don't own!");
            return null;
        }

        /* Return the hand object */
        return hand;
    }

    protected override bool OnBeforeDropped(GameObject obj, HandManager hand)
    {
        /* Get the item component from the object */
        SojournItem item = obj.GetComponent<SojournItem>();
        if (item == null)
            return false;

        /* Is this our player journal? */
        if (item.GetComponent<JournalManager>() == null || !item.hasAuthority)
            return false;

        if (debug) Debug.Log("PlayerToolbeltManager Client: We returned the player journal!");

        /* The player journal has to return to it's slot */
        PlayerToolbeltSlot slot = toolbelt.ReturnItem(item);
        if(slot == null)
        {
            if (debug) Debug.Log("PlayerToolbeltManager Client: Failed to return restricted item!");
            return false;
        }

        /* Deliver the toolbet event */
        eventManager.OnToolbeltEvent(VRInteraction.InteractionEvent.OnItemPlacedInToolbelt, obj, slot, hand);

        /* We handled this event */
        return true;
    }

    protected override bool OnItemDroppedOnInteractionBox(InteractionBox box, GameObject obj, HandManager hand)
    {
        /* See if this object is a sojourn item */
        SojournItem item = obj.GetComponent<SojournItem>();

        /* Is this an item? */
        if (item == null)
            return false;

        /* Is this the relic or the journal? */
        bool relic = obj.GetComponent<PlayerRelic>() != null;
        bool journal = obj.GetComponent<JournalManager>() != null;

        /* Are we dropping something into a slot? */
        PlayerToolbeltSlot slot = box.GetGameObject().GetComponent<PlayerToolbeltSlot>();
        if(slot == null)
        {
            /* This event isn't consumable by us */
            return false;
        }

        /* Do we own this toolbelt slot? */
        if(!slot.GetSocketManager().hasAuthority)
        {
            /* We don't own this toolbelt slot */
            if (debug) Debug.Log("PlayerToolbeltManager: We don't own this toolbelt slot!");
            return false;
        }

        /* Is this slot occupied? */
        if(slot.IsOccupied())
        {
            if (debug) Debug.Log("PlayerToolbeltManager: Toolbelt slot is occupied.");
            return false;
        }

        if (debug) Debug.Log("PlayerToolbeltManager Client: Player dropped something onto a slot!");

        /* If this is the relic or the journal, return this to the slot where it's supposed to be */
        if(relic || journal)
        {
            /* Return this item to where it's supposed to go */
            if(!toolbelt.ReturnItem(item))
                Debug.LogError("PlayerToolbeltManager: Failed to return item to it's correct slot.");
        }

        /* Are we overlapping with a slot? */
        if (!slot.socketRestricted && !slot.IsOccupied())
        {
            if (debug) Debug.Log("PlayerToolbeltManager Client: We're dropping something onto a slot!");

            /* Attach this to something else */
            if (!item.ClientReattach(slot))
            {
                Debug.LogError("PlayerToolbeltManager Client: Failed to attach item to socket!");
                return false;
            }

            /* Deliver the toolbet event */
            eventManager.OnToolbeltEvent(VRInteraction.InteractionEvent.OnItemPlacedInToolbelt, obj, slot, hand);

            /* This event was handled */
            return true;
        }

        /* Is the player returning a restricted item? */
        if (slot.socketRestricted && slot.AcceptsAttachable(item))
        {
            /* Return the item to the toolbelt. */
            if (!slot.ReturnItem(item))
            {
                if (debug) Debug.Log("PlayerToolbeltManager Client: Failed to return restricted item!");
                return false;
            }

            if (debug) Debug.Log("PlayerToolbeltManager Client: We returned a restricted item!");

            /* Deliver the toolbet event */
            eventManager.OnToolbeltEvent(VRInteraction.InteractionEvent.OnItemPlacedInToolbelt, obj, slot, hand);

            /* The item was returned */
            return true;
        }

        if (debug) Debug.Log("PlayerToolbeltManager Client: We aren't overlapping with an open toolbelt slot.");

        /* We couldn't consume this event */
        return false;
    }

    public bool ReturnItem(SojournItem item) { return toolbelt.ReturnItem(item); }
    public void SetToolbelt(PlayerToolbelt toolbelt) { this.toolbelt = toolbelt; }
    public bool IsItemInToolbelt(SojournItem item) { return toolbelt.GetItems().Contains(item); }
}
