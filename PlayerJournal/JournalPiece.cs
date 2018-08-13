using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JournalPiece : MonoInteractionBox
{
    [Tooltip("Whether or not this is the right half of the journal")]
    public bool isJournalRight;

    /* The rotations to move towards when the book is open */
    private Quaternion journalOpen;

    /* The rotations to move the journal pieces towards when the book is closed */
    private Quaternion journalClosed;

    /* The value that helps us with the journal animation (0.0 is closed, 1.0 is open)*/
    private float journalLerpValue;

    /* The speed at which the journal opens and closes (higher is faster) */
    public static readonly float journalLerpSpeed = 2.0f;

    private void Awake()
    {
        /* Save the open rotation */
        journalOpen = transform.localRotation;

        /* Create the closed rotation */
        if(isJournalRight)
            journalClosed = Quaternion.Euler(-270.0f, 180.0f, -180.0f);
        else journalClosed = Quaternion.Euler(-90.0f, 180.0f, 0.0f);

        /* The journal starts open */
        journalLerpValue = 1.0f;
    }

    public bool JournalUpdate(bool opening)
    {
        /* Whether or not we're done animating */
        bool done = false;

        /* Are we opening? */
        if (opening)
        {
            /* Move the lerp value closer to the open state */
            journalLerpValue += Time.deltaTime * journalLerpSpeed;

            /* Are we all the way open? */
            if (journalLerpValue >= 1.0f)
            {
                /* We are all the way open */
                journalLerpValue = 1.0f;
                done = true;
            }
        } else
        {
            /* Move the lerp value closer to the closed state */
            journalLerpValue -= Time.deltaTime * journalLerpSpeed;

            /* Are we closed all the way? */
            if (journalLerpValue <= 0.0f)
            {
                /* We are all the way closed */
                journalLerpValue = 0.0f;
                done = true;
            }
        }
        
        /* Update our rotation */
        transform.localRotation = Quaternion.Slerp(journalClosed, journalOpen, journalLerpValue);

        return done;
    }

    public float GetLerpValue() { return journalLerpValue; }
}
