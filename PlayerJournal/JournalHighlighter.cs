using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JournalHighlighter : HighlighterBase, IHighlightingTarget
{
    [Tooltip("The color that will be used for highlighting")]
    public Color highlightColor;

    private static bool debug = false;

    protected override void Awake()
    {
        base.Awake();

        /* Disable the highlighter */
        h.Off();
    }

    public void EnableHighlighting()
    {
        if (debug) Debug.Log("PlayerJournalHighlighter: Enabling highlighting...");

        /* Enable the highlighter */
        h.ConstantOn(highlightColor);
    }

    public void DisableHighlighting()
    {
        if (debug) Debug.Log("PlayerJournalHighlighter: Disabling highlighting...");

        /* Enable the highlighter */
        h.Off();
    }

    public void OnHighlightingFire1Down() { }
    public void OnHighlightingFire1Held() { }
    public void OnHighlightingFire1Up() { }
    public void OnHighlightingFire2Down() { }
    public void OnHighlightingFire2Held() { }
    public void OnHighlightingFire2Up() { }
    public void OnHighlightingMouseOver() { }
}
