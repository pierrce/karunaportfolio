using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestsRightPage : JournalPage
{
    public Text objectivesText;

    private string completed_text = "#008000FF";

    protected override void PageAwake()
    {
        if (objectivesText == null)
            throw new System.Exception("You have to set quest objectives text!");
        objectivesText.text = "";
    }

    public void ClearObjectives()
    {
        objectivesText.text = "";
    }

    public void AddCompletedObjective(string objective)
    {
        if (!string.IsNullOrEmpty(objectivesText.text))
            objectivesText.text += "\n";
        objectivesText.text += "<color=" + completed_text + ">" + objective + "</color>";
    }

    public void AddObjective(string objective)
    {
        if (!string.IsNullOrEmpty(objectivesText.text))
            objectivesText.text += "\n";
        objectivesText.text += objective;
    }

}
