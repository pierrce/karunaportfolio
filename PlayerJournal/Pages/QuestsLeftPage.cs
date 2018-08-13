using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestsLeftPage : JournalPage
{
    public Text questTitleText;
    public Text questDescriptionText;

    private string completed_text = "#008000FF";

    protected override void PageAwake()
    {
        if (questTitleText == null)
            throw new System.Exception("You have to set a quest title!");
        if (questDescriptionText == null)
            throw new System.Exception("You have to set a quest description!");
    }

    public void SetQuestInformation(string title, string description, bool complete)
    {
        /* Set the description */
        questDescriptionText.text = description;

        /* Set the title */
        if (complete)
            questTitleText.text += title + " <color=" + completed_text + ">(Complete)</color>";
        else questTitleText.text = title;
    }
}
