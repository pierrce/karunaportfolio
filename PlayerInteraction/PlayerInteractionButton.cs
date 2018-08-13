using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionButton : UIButton
{
    public enum ButtonType
    {
        None,
        SendFriendRequest,
        MutePlayer,
        BlockPlayer,
        ReportPlayer
    };

    public override bool IsCompatibleWithManager(Component component)
    {
        // return component is PlayerInteractionUIMenuManager;
        return false;
    }
}
