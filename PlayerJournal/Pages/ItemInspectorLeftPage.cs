using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInspectorLeftPage : JournalPage
{
    public Text itemNameText;
    public Text itemWeightValueText;
    public Text itemPropertiesText;
    public SojournItemSocket itemSocket;

    protected override void PageAwake()
    {
        if (itemNameText == null)
            throw new System.Exception("Item name needs to be set!");
        if (itemWeightValueText == null)
            throw new System.Exception("Item weight/value needs to be set!");
        if (itemPropertiesText == null)
            throw new System.Exception("Item properties needs to be set!");
        if (itemSocket == null)
            throw new System.Exception("Item socket needs to be set!");
    }

    protected override void OnCullPage()
    {
        /* Clear the page information */
        Clear();
    }

    public void Clear()
    {
        /* Make sure the item socket is clear */
        if(itemSocket.IsOccupied())
            itemSocket.ServerDestroyAttachedItem();

        itemNameText.text = "Item Inspector";
        itemWeightValueText.text = "Weight:  Value: ";
        itemPropertiesText.text = "Please pickup an item with your other hand to inspect something.";
    }

    public void InspectItem(Item item)
    {
        /* Make sure the item socket is open */
        Clear();
        Debug.Assert(itemSocket.GetAttachedObject() == null);

        /* Set some of the properties */
        itemNameText.text = item.item_name;
        itemWeightValueText.text = "Weight: " + item.item_value + " Value: " + item.item_value;
        itemPropertiesText.text = "";

        /* Display the item */
        itemSocket.CreateLocalItemInSocket(item);

        /* Transform the item */
        SojournItem sojournItem = itemSocket.GetAttachedItem();
        sojournItem.PlacedInInventory();
    }
}
