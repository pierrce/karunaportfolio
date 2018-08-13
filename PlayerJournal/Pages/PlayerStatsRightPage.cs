using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsRightPage : JournalPage
{
    public Text statusEffectsText;

    public void UpdateStatusEffects(EntityCombatManager combat)
    {
        ///* Let's generate the status effect string */
        //string status_string = "";
        //List<StatusEffect> effects = combat.GetStatusEffects();

        //foreach(StatusEffect e in effects)
        //{
        //    /* Is the value positive or negative? */
        //    string pos_neg = "+";
        //    if (e.effect_value < 0)
        //        pos_neg = "-";

        //    /* Get the absolute value of this effect */
        //    int value = Mathf.Abs(e.effect_value);

        //    /* Should we append a newline? */
        //    if (!string.IsNullOrEmpty(status_string))
        //        status_string += "\n";

        //    /* Append the status string */
        //    status_string += pos_neg + value + " " + StatusEffect.EffectToName(e.effect_type);
        //}

        //statusEffectsText.text = status_string;
    }
}
