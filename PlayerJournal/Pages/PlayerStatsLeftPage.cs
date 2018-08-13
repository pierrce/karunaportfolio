using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsLeftPage : JournalPage
{
    public Text playerNameText;
    public Text levelsText;

    public Image healthProgress;
    public Image manaProgress;
    public Image staminaProgress;
    public Image strengthProgress;
    public Image defenseProgress;
    public Image magickProgress;
    public Image miningProgress;
    public Image smithingProgress;
    public Image smeltingProgress;
    public Image alchemyProgress;
    public Image cookingProgress;
    public Image woodWorkingProgress;
    public Image woodCuttingProgress;

    private EntitySkills skills;

    protected override void PageAwake()
    {
        skills = new EntitySkills();

        playerNameText.text = "";
        levelsText.text = "";
    }

    public void UpdatePlayerName(string username)
    {
        playerNameText.text = username;
    }

    public void UpdatePlayerSkills(EntitySkills skills)
    {
        /* Save these skills */
        this.skills.ApplyChanges(skills);

        /* Update text and progress bars for skills */
        UpdateSkillTexts();
        UpdateSkillProgress();
    }

    private void UpdateSkillTexts()
    {
        /* Get each of the levels */
        string levels = "" + skills.GetHealthSkill();
        levels += "\n" + skills.GetManaSkill();
        levels += "\n" + skills.GetStaminaSkill();
        levels += "\n" + skills.GetStrengthSkill();
        levels += "\n" + skills.GetDefenseSkill();
        levels += "\n" + skills.GetMagickSkill();
        levels += "\n" + skills.GetMiningSkill();
        levels += "\n" + skills.GetSmithingSkill();
        levels += "\n" + skills.GetSmeltingSkill();
        levels += "\n" + skills.GetAlchemySkill();
        levels += "\n" + skills.GetCookingSkill();
        levels += "\n" + skills.GetWoodworkingSkill();
        levels += "\n" + skills.GetWoodcuttingSkill();

        /* Assign the levels to the text */
        levelsText.text = levels;
    }

    private void UpdateSkillProgress()
    {
        /* Set all of the fill amounts */
        healthProgress.fillAmount = skills.GetHealthProgress();
        manaProgress.fillAmount = skills.GetManaProgress();
        staminaProgress.fillAmount = skills.GetStaminaProgress();
        strengthProgress.fillAmount = skills.GetStrengthProgress();
        defenseProgress.fillAmount = skills.GetDefenseProgress();
        magickProgress.fillAmount = skills.GetMagickProgress();
        miningProgress.fillAmount = skills.GetMiningProgress();
        smithingProgress.fillAmount = skills.GetSmithingProgress();
        smeltingProgress.fillAmount = skills.GetSmeltingProgress();
        alchemyProgress.fillAmount = skills.GetAlchemyProgress();
        cookingProgress.fillAmount = skills.GetCookingProgress();
        woodWorkingProgress.fillAmount = skills.GetWoodworkingProgress();
        woodCuttingProgress.fillAmount = skills.GetWoodcuttingProgress();
    }
}
