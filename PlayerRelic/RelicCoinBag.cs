using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RelicCoinBag : MonoInteractable
{
    [Tooltip("The text that displays the amount of money in the coin bag")]
    public TextMesh amount;

    /* Whether or not we are currently lerping to a new position */
    private bool lerping;
    private Transform lerpTransform;

    /* The position and rotation we started in */
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool lerpingHome;

    /* The speed of the lerp for the coin bag */
    private readonly float lerpSpeed = 2f;

    private void Awake()
    {
        /* Amount is required */
        if (amount == null)
            throw new System.Exception("Amount is required!");

        homePosition = transform.localPosition;
        homeRotation = transform.localRotation;
    }

    public void LerpHome()
    {
        lerping = true;
        lerpingHome = true;
    }

    /**
     * Tells the coin bag to lerp to a new position and rotation.
     */
    public void LerpTo(Transform lerpTransform)
    {
        this.lerpTransform = lerpTransform;
        lerping = true;
        lerpingHome = false;
    }

    /**
     * Shows an amount below the coin bag.
     */
    public void ShowAmount(long number)
    {
        amount.text = "" + number;
    }

    /**
     * Hides the amount below the coin bag.
     */
    public void HideAmount()
    {
        amount.text = "";
    }

    private void Update()
    {
        if (!lerping)
            return;

        if(lerpingHome)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, homePosition, lerpSpeed);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, homeRotation, lerpSpeed);
        } else
        {
            transform.position = Vector3.Lerp(transform.position, lerpTransform.position, lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, lerpTransform.rotation, lerpSpeed);
        }
    }

    public override bool IsCompatibleWithAction(VRInteraction.InteractionEvent action) { return action == VRInteraction.InteractionEvent.OnTriggerPressed; }
    public override bool IsCompatibleWithManager(Component component) { return component is PlayerRelicManager; }
}
