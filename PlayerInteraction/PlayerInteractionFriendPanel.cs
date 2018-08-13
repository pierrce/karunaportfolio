using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionFriendPanel : MonoBehaviour
{
    public PlayerInteractionButton sendFriendRequestButton;
    public PlayerInteractionButton mutePlayerButton;
    public PlayerInteractionButton blockPlayerButton;
    public PlayerInteractionButton reportPlayerButton;

    public PlayerInteractionButton.ButtonType OwnsButton(PlayerInteractionButton button)
    {
        if (button == sendFriendRequestButton)
            return PlayerInteractionButton.ButtonType.SendFriendRequest;
        if (button == mutePlayerButton)
            return PlayerInteractionButton.ButtonType.MutePlayer;
        if (button == blockPlayerButton)
            return PlayerInteractionButton.ButtonType.BlockPlayer;
        if (button == reportPlayerButton)
            return PlayerInteractionButton.ButtonType.ReportPlayer;

        return PlayerInteractionButton.ButtonType.None;
    }
}
