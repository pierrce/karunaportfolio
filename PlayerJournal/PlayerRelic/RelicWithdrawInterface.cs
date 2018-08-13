using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Normal.UI;

public class RelicWithdrawInterface : MonoBehaviour
{
    /* The keyboard attached to the relic */
    public Keyboard keyboard;
    public GameObject moneyPouch;
    public Transform lerpPosition;
    public RelicCoinBag coinBag;

    /* The player inventory manager for the player that we're managing */
    [HideInInspector]
    public PlayerInventoryManager inventoryManager;

    /* The display attached to the keyboard */
    private KeyboardDisplay keyboardDisplay;

    /* The mallet that attaches to the left hand for typing */
    public GameObject leftMallet;
    /* The mallet that attaches to the right hand for typing */
    public GameObject rightMallet;

    /* Whether or not we are withdrawing coins from the player's inventory */
    private bool isWithdrawing;

	private void Start ()
    {
        if (keyboard == null)
            throw new System.Exception("You must set a keyboard!");
        if (moneyPouch == null)
            throw new System.Exception("You must set a money pouch!");
        if (coinBag == null)
            throw new System.Exception("You must set a coin bag!");

        /* Get the keyboard display */
        keyboardDisplay = keyboard.GetComponentInChildren<KeyboardDisplay>();
        Debug.Assert(keyboardDisplay != null);

        /* Doesn't show keypad because we aren't withdrawing */
        isWithdrawing = false;

        /* Disable the keyboard for now */
        keyboard.gameObject.SetActive(false);
        /* Add our callback */
        keyboard.keyPressed += KeyPressed;
    }

    /**
     * If the pouch is clicked on, the keypad is shown and the lerp occurs 
     */
    public void OnTriggerClick(HandManager hand, PlayerInventory inventory)
    {
        leftMallet.GetComponent<KeyboardMallet>().SetTrackedObject(GameObject.Find("Controller (left)").GetComponent<SteamVR_TrackedObject>());
        rightMallet.GetComponent<KeyboardMallet>().SetTrackedObject(GameObject.Find("Controller (right)").GetComponent<SteamVR_TrackedObject>());

        /* If a transaction is about to occur, enable the keypad and lerp the bag */
        if (!isWithdrawing)
        {
            /* Enable the keypad, the lerp and we're now withdrawing */
            keyboard.gameObject.SetActive(true);
            isWithdrawing = true;
            coinBag.LerpTo(lerpPosition);
        } else
        {
            /* If a transaction is ending, disable the keypad and lerp the bag back home */
            keyboard.gameObject.SetActive(false);

            /* Get the text from the keyboard */
            Text t = keyboardDisplay.getText();
            long l = System.Convert.ToInt64(t.text);

            /* The player cannot take out more coins than they have in their inventory */
            if (inventory.gold < l)
                l = inventory.gold;

            /* Reset the display on the keyboard */
            t.text = "";

            /* Spawn the object in the player's hand */
            // hand.GetSocket(0).SpawnItem("Small Coin Bag", hand.GetPlayerController());
            // hand.GetSocket(0).GetAttachedObject().GetComponent<SojournGoldBag>().SetValue(l);

            /* Enable lerping, and we're done withdrawing */
            coinBag.LerpHome();
            isWithdrawing = false;
        }
    }

    public void KeyPressed(Keyboard keyboard, string keyPress)
    {
        /* Get the maximum amount of coins we can withdraw from the inventory */
        long maxCoins = inventoryManager.GetInventory().gold;

        /* Get the current text for the keyboard display */
        string text = keyboardDisplay.getText().text;

        if (string.IsNullOrEmpty(text))
            return;

        /* Make sure we're not over the maximum amount of coins. */
        long coins = long.Parse(text);
        if (coins > maxCoins)
            coins = maxCoins;

        /* Update the text for the keyboard display s*/
        keyboardDisplay.getText().text = "" + coins;
    }
}
