using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInspectorRightPage : JournalPage
{
    [Tooltip("The text box for the description of the item that is being inspected.")]
    public Text descriptionText;

    protected override void PageAwake()
    {
        descriptionText.text = "";
    }

    public void Clear()
    {
        descriptionText.text = "";
    }

    public void SetDescription(string description)
    {
        descriptionText.text = description;
    }
}
