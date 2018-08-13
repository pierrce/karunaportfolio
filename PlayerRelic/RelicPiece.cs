using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelicPiece : MonoBehaviour
{
    [Tooltip("The position the piece should be in when the relic is completely open")]
    [UnityEngine.Serialization.FormerlySerializedAs("open_position")]
    public Vector3 inventoryPosition;
    [Tooltip("The position the piece should be in when the bank is completely open")]
    public Vector3 bankPosition;

    /* The closed position of the relic piece */
    private Vector3 closedPosition;

    /* The distance at which this piece will snap to position */
    private readonly float distance_allowance = 0.001f;

    /* Speed used for relic lerps */
    public readonly float speed = 8.0f;

    protected void Awake()
    {
        /* Save the close position for this relic piece */
        closedPosition = transform.localPosition;
    }

    /**
     * Called when this relic piece should move towards it's destination.
     */
    public bool PieceUpdate(PlayerRelic.RelicConfiguration config)
    {
        switch(config)
        {
            case PlayerRelic.RelicConfiguration.Closed:
                transform.localPosition = Vector3.Lerp(transform.localPosition, closedPosition, speed * Time.deltaTime);
                if (Vector3.Distance(transform.localPosition, closedPosition) < distance_allowance)
                {
                    /* We are at our destination */
                    transform.localPosition = closedPosition;
                    return false;
                }
                break;
            case PlayerRelic.RelicConfiguration.OpenInventory:
                transform.localPosition = Vector3.Lerp(transform.localPosition, inventoryPosition, speed * Time.deltaTime);
                if (Vector3.Distance(transform.localPosition, inventoryPosition) < distance_allowance)
                {
                    /* We are at our destination */
                    transform.localPosition = inventoryPosition;
                    return false;
                }
                break;
            case PlayerRelic.RelicConfiguration.OpenBank:
                transform.localPosition = Vector3.Lerp(transform.localPosition, bankPosition, speed * Time.deltaTime);
                if (Vector3.Distance(transform.localPosition, bankPosition) < distance_allowance)
                {
                    /* We are at our destination */
                    transform.localPosition = bankPosition;
                    return false;
                }
                break;
            default:
                Debug.LogError("Unsupported state: " + config);
                break;
        }

        /* We're not done moving */
        return true;
    }
}
