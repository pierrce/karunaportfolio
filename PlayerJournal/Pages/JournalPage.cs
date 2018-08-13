using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class JournalPage : MonoBehaviour
{
    [Tooltip("The canvas that holds the text and other components for the page")]
    public GameObject canvas;
    [Tooltip("Whether or not this is a right page.")]
    public bool isRight;

    public enum JournalPageState
    {
        OpeningWithJournal, /* This page is opening with the journal */
        FlippedToRest, /* Going from its flipped state to its resting state */
        RestToFlipped, /* Going from its resting state to its flipped state */

        CreatedResting, /* The page was created in the resting state */
        CreatedFlippingToResting /* The page was created in the flipping state */
    }

    /* The rotation the page should have when the journal is open */
    private Quaternion pageRestingRotation;
    /* The rotation this page should have when the page is done flipping */
    private Quaternion pageFlippedRotation;

    /* The lerp value for this page (1.0 is resting state, 0.0 is flipped)*/
    private float pageLerpValue;
    /* The value that the page is lerping to */
    private float pageLerpTarget;
    /* The speed of the lerp for this page */
    private float pageLerpSpeed;
    /* Whether or not this page is culled */
    private bool pageCulled;

    /* The page pair that we're in */
    private PageManager.PagePairing pairing;

    /* The mesh renderer for this page */
    private MeshRenderer mesh;

    /* The speed at which pages flip */
    private readonly float page_flip_speed = 2.0f;
    /* The point at which the page is culled while flipping */
    private readonly float page_flip_cull = 0.05f;

    private readonly bool debug = false;

    protected void Awake()
    {
        /* Get the renderer for this page */
        mesh = GetComponentInChildren<MeshRenderer>();
        if (mesh == null)
            throw new System.Exception("Mesh renderer cannot be null!");

        if (isRight)
        {
            /* Set the page rotations for a right page */
            pageRestingRotation = Quaternion.Euler(-205.0f, 180.0f, -180.0f);
            pageFlippedRotation = Quaternion.Euler(-345.0f, 180.0f, -180.0f);
        } else
        {
            /* Set the page rotations for a left page */
            pageRestingRotation = Quaternion.Euler(-345.0f, 180.0f, -180.0f);
            pageFlippedRotation = Quaternion.Euler(-205.0f, 180.0f, -180.0f);
        }

        /* This page starts as not culled */
        pageCulled = false;

        /* Call awake for our child */
        PageAwake();
    }
    protected virtual void PageAwake() { }

    public void SetState(JournalPageState state)
    {
        /* If this is an opening with journal state, this function shouldn't be used. */
        switch(state)
        {
            case JournalPageState.OpeningWithJournal:
                Debug.LogError("JournalPage: For better accuracy, pass the journal lerp value.");
                break;
        }

        /* Set our new state */
        SetState(state, 0.0f);
    }

    public void SetState(JournalPageState state, float journalLerpValue)
    {
        /* Uncull this page */
        pageCulled = false;
        mesh.enabled = true;
        canvas.SetActive(true);

        switch(state)
        {
            case JournalPageState.OpeningWithJournal:
                pageLerpValue = (journalLerpValue * 0.5f) + 0.5f - 0.1f;
                pageLerpTarget = 1.0f;
                pageLerpSpeed = JournalPiece.journalLerpSpeed / 2.0f;
                break;
            case JournalPageState.FlippedToRest:
                pageLerpTarget = 1.0f;
                pageLerpSpeed = page_flip_speed;
                break;
            case JournalPageState.RestToFlipped:
                /**
                 * We hide the page when we start flipping because the new
                 * page will start Z fighting with that, and we don't want
                 * that to happen.
                 */
                HidePage();
                pageLerpTarget = 0.0f;
                pageLerpSpeed = page_flip_speed;
                break;

            case JournalPageState.CreatedResting:
                pageLerpValue = pageLerpTarget = 1.0f;
                break;
            case JournalPageState.CreatedFlippingToResting:
                pageLerpValue = 0.0f;
                pageLerpTarget = 1.0f;
                pageLerpSpeed = page_flip_speed;
                break;
        }

        /* Update the rotation of the page so that the first frame doesn't studder */
        UpdateRotation();
    }

    public void Update()
    {
        /* Are we at our destination? */
        if (pageLerpValue == pageLerpTarget || pageCulled)
            return;

        if (pageLerpValue > pageLerpTarget)
        {
            /* Decrease the value */
            pageLerpValue -= Time.deltaTime * pageLerpSpeed;

            /* Did we go past our target? */
            if (pageLerpValue < pageLerpTarget)
                pageLerpValue = pageLerpTarget;
        } else
        {
            /* Increase the value */
            pageLerpValue += Time.deltaTime * pageLerpSpeed;

            /* Did we go past our target? */
            if (pageLerpValue > pageLerpTarget)
                pageLerpValue = pageLerpTarget;
        }

        /* Should we be culled? */
        if (pageLerpTarget == 0.0f && pageLerpValue <= page_flip_cull)
        {
            /* Cull this page pairing */
            pairing.pageLeft.CullPage();
            pairing.pageRight.CullPage();
            return;
        }

        /* Update the rotation of this page */
        UpdateRotation();
    }

    private void UpdateRotation()
    {
        /* Update our rotation */
        transform.localRotation = Quaternion.Slerp(pageFlippedRotation, pageRestingRotation, pageLerpValue);
    }

    /**
     * Cull this page. This page will not be drawn and
     * the text for the page will not be drawn, however
     * the script will still be enabled and ready for
     * when the page is needed again.
     */
    public void CullPage()
    {
        mesh.enabled = false;
        canvas.SetActive(false);
        pageCulled = true;

        /* Do child callback */
        OnCullPage();

        if (debug) Debug.Log("JournalPage: Page culled: " + name);
    }
    protected virtual void OnCullPage() { }

    public void HidePage() { mesh.enabled = false; }
    public void SetPagePairing(PageManager.PagePairing pairing) { this.pairing = pairing; }
}
