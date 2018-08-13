using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerRelic : SojournItem
{
    [Header("Bank Positions")]

    [Tooltip("The position of the bank UI when the bank is open")]
    public GameObject bankUIPosition;
    [Tooltip("The position of the inventory UI when the bank is open")]
    public GameObject bankUIRelicPosition;
    [Tooltip("The light object for this relic")]
    public GameObject lightObj;

#if UNITY_EDITOR
    [Header("Relic Debug")]

    public bool _openInventory;
    public bool _openBank;
    public bool _close;
#endif

    /**
     * The possible positions for the relic.
     */
    public enum RelicConfiguration
    {
        OpenInventory,
        OpenBank,
        Closed
    };

    private struct RelicState
    {
        public RelicState(RelicConfiguration config, Vector3 position)
        {
            this.config = config;
            this.position = position;
        }

        public static RelicState ClosedState()
        {
            return new RelicState(RelicConfiguration.Closed, Vector3.zero);
        }

        public static RelicState OpenInventory(Vector3 position)
        {
            return new RelicState(RelicConfiguration.OpenInventory, position);
        }

        public static RelicState OpenBank()
        {
            return new RelicState(RelicConfiguration.OpenBank, Vector3.zero);
        }

        /* The current configuration of the relic */
        public readonly RelicConfiguration config;
        /* The position the relic should be opening to */
        public readonly Vector3 position;
    }

    /**
     * The state of the relic.
     */
    [SyncVar(hook = "RelicStateUpdated")]
    private RelicState state;

    /* Whether or not there are still moving relic pieces */
    private bool updating;

    /* The pieces of the relic */
    private RelicPiece[] pieces;

    /* The player who owns us */
    private PlayerRelicManager player;

    /* Speed at which the relic lerps with */
    private readonly float lerp_speed = 5.0f;
    /* The distance at which the lerp will snap */
    private readonly float lerp_snap = 0.01f;

    protected override void ItemAwake()
    {
        base.ItemAwake();

        /* Set the initial state. On clients this gets overridden in OnStartClient. */
        state = RelicState.ClosedState();

        /* Get all of the relic pieces */
        pieces = GetComponentsInChildren<RelicPiece>();

        /* The relic isn't updating right now */
        updating = false;
    }

    protected override void ItemOnStartClient()
    {
        /* Move pieces to the correct position */
        updating = true;
    }

    /**
     * Called when the server updates the state of this relic.
     */
    [Client]
    private void RelicStateUpdated(RelicState state)
    {
        this.state = state;
        updating = true;

        /* Let the player know that the relic is closing if we need to */
        if (state.config == RelicConfiguration.Closed && player != null)
            player.RelicClosing();

        switch(state.config)
        {
            case RelicConfiguration.Closed:
                lightObj.SetActive(true);
                break;
            case RelicConfiguration.OpenBank:
            case RelicConfiguration.OpenInventory:
                lightObj.SetActive(false);
                break;
        }
    }

    /**
     * Opens the relic in the inventory configuration.
     */
    [Client]
    public bool ClientOpenInventory(Vector3 position)
    {
        if(!hasAuthority)
        {
            Debug.LogError("PlayerRelic: We don't have authority over this object!");
            return false;
        }

        /* Open the inventory for others */
        CmdOpenInventory(position);
        
        /* We sent the command */
        return true;
    }
    [Command]
    private void CmdOpenInventory(Vector3 position)
    {
        /* Update the relic state for everyone */
        state = RelicState.OpenInventory(position);
        updating = true;
    }

    /**
     * Opens the relic in the bank configuration.
     */
    [Client]
    public bool ClientOpenBank()
    {
        if(!hasAuthority)
        {
            Debug.LogError("PlayerRelic: We don't have authority over this object!");
            return false;
        }

        /* Use a command to tell the server to open the player's bank */
        CmdOpenBank();

        /* We sent the command */
        return true;
    }
    [Command]
    private void CmdOpenBank()
    {
        /* Update the relic state for everyone */
        state = RelicState.OpenBank();
        updating = true;
    }

    /**
     * Close the relic. This can only be called from the client.
     */
    [Client]
    public bool ClientClose()
    {
        if(!hasAuthority)
        {
            Debug.LogError("PlayerRelic: We don't have authority over this object!");
            return false;
        }

        /* Close the relic */
        CmdClose();

        /* Command was sent */
        return true;
    }
    [Command]
    private void CmdClose()
    {
        /* Update the relic state */
        state = RelicState.ClosedState();
    }

    /**
     * Sets the player relic manager for this relic.
     */
    public void SetRelicManager(PlayerRelicManager player)
    {
        this.player = player;
    }

    protected override void ItemUpdate()
    {
        /* Are the pieces updating? */
        if (updating)
        {
            /* Set updating to false initially. In any piece isn't done moving, this will be set to true */
            updating = false;

            /* Update all of these pieces */
            foreach (RelicPiece p in pieces)
                updating |= p.PieceUpdate(state.config);

            /* Lerp the position if the state is not closed */
            if (state.config == RelicConfiguration.OpenInventory)
            {
                transform.position = Vector3.Lerp(transform.position, state.position, lerp_speed * Time.deltaTime);
                if (Vector3.Distance(transform.position, state.position) > lerp_snap)
                    updating = true;
            }

            /* Are we done opening? Let the player know. */
            if (!updating && state.config != RelicConfiguration.Closed && isClient && player != null)
                player.RelicOpened(state.config);
        }

        /* Rotate the relic if we need to */
        if (player != null)
        {
            switch (state.config)
            {
                case RelicConfiguration.OpenInventory:

                    /* Rotate the relic towards our owner */
                    Vector3 rotatePosition = player.transform.position;
                    rotatePosition.y = transform.position.y;

                    /* Look at the player who owns us */
                    transform.LookAt(rotatePosition);
                    break;
            }
        }

#if UNITY_EDITOR
        if(_openInventory)
        {
            ClientOpenInventory(transform.position);
            _openInventory = false;
        }

        if (_openBank)
        {
            ClientOpenBank();
            _openBank = false;
        }

        if (_close)
        {
            ClientClose();
            _close = false;
        }
#endif
    }

    public bool IsOpen() { return state.config != RelicConfiguration.Closed; }
    public bool IsClosed() { return state.config == RelicConfiguration.Closed; }
}
