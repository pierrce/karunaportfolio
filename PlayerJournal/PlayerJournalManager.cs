using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PlayerJournalManager : InteractionManager
{
    /* Our quest manager */
    private PlayerQuestManager questManager;

    /* The network id of the journal that this player owns */
    [SyncVar(hook = "JournalUpdated")]
    private NetworkInstanceId currentJournalId = NetworkInstanceId.Invalid;
    /* The current questbook being held */
    private JournalManager currentJournal;
    

    /* Whether or not we're inspecting an item */
    private bool inspecting;
    /* Our page manager state before we started inspecting an item*/
    private PageManager.PagePairing beforeInspecting;

    /* The speed that pages rotate at */
    private const float rotation_speed = 2.4f;

    /* The combat manager for this player */
    private EntityCombatManager combatManager;
    /* The skills manager for this player */
    private PlayerSkillsManager skillsManager;

    /* Whether or not to print debug for this class */
    private static readonly bool debug = false;

    protected override void AfterAwake()
    {
        /* Get the combat manager for this player */
        combatManager = GetComponent<EntityCombatManager>();
        if (combatManager == null)
            throw new System.Exception("Why does this player not have a combat manager?");

        /* Get the player skills manager for this player */
        skillsManager = GetComponent<PlayerSkillsManager>();
        if (skillsManager == null)
            throw new System.Exception("Why does this player not have a player skills manager?");

        /* Get our quest manager */
        questManager = GetComponent<PlayerQuestManager>();
        if (questManager == null)
            throw new System.Exception("There is no quest manager attached to this player!");

        /* We're not inspecting any item right now */
        inspecting = false;
        beforeInspecting = null;
    }

    public override void OnStartClient()
    {
        /* Call hook manually if needed */
        if (currentJournalId != NetworkInstanceId.Invalid)
            JournalUpdated(currentJournalId);
    }

    private void JournalUpdated(NetworkInstanceId currentJournalId)
    {
        /* Don't process hooks on local hosts */
        if (isServer && isClient)
            return;

        // Debug.LogError("JOURNAL HOOK= " + currentJournalId.Value);

        /* Save the journal id */
        this.currentJournalId = currentJournalId;

        /* Get the journal object */
        GameObject journalObj = ClientScene.FindLocalObject(currentJournalId);
        Debug.Assert(journalObj != null);
        /* Get the journal component */
        currentJournal = journalObj.GetComponent<JournalManager>();
        Debug.Assert(currentJournal != null);

        /* Setup the page manager for this journal */
        currentJournal.SetupPageManager(playerController.GetEntityName(), questManager,
            skillsManager.GetSkills(), combatManager);

        if (debug) Debug.Log("PlayerJournalManager Client: Journal assigned.");
    }

    protected override void OnLevelGained(EntitySkills.Skill skill, EntitySkills skills)
    {
        if (currentJournal == null)
            return;

        /* Make the book have a highlight */
        if(currentJournal.GetAttachedHand() == null)
        {
            /* Turn on highlighting */
            currentJournal.EnableHighlight();
        }
    }

    [Server]
    protected override bool OnServerItemSpawnedInToolbelt(GameObject obj, PlayerToolbeltSlot slot)
    {
        /* Get the journal manager from this item */
        JournalManager journal = obj.GetComponent<JournalManager>();

        /* Does this item have a journal manager? */
        if(journal != null)
        {
            /* Set the current journal */
            currentJournal = journal;
            currentJournalId = journal.netId;

            /* Setup the journal on clients only */
            if (isClient)
            {
                currentJournal.SetupPageManager(playerController.GetEntityName(), questManager,
                    skillsManager.GetSkills(), combatManager);
            }

            if (debug) Debug.Log("PlayerJournalManager: Journal assigned.");

            /* We consumed this event */
            return true;
        }

        /* We don't care about this item */
        return false;
    }

    protected override void OnAfterPickedUp(GameObject obj, HandManager hand)
    {
        if (!isClient)
            return;

        /* We need to have a journal */
        if (currentJournal == null)
            return;

        /* Is this the player relic? */
        if (obj.GetComponent<PlayerRelic>() != null)
            return;

        /* Convert the obj to a sojourn item */
        SojournItem item = obj.GetComponent<SojournItem>();

        /* Is this our journal? */
        if(currentJournal.gameObject == obj)
        {
            if (debug) Debug.Log("PlayerJournalManager: Picked up the Player Journal!");

            /* Update journal stats just in case */
            currentJournal.UpdateStats(playerController.GetEntityName(), skillsManager.GetSkills(), combatManager);

            /* Open the journal */
            currentJournal.OpenJournal();

            /* Disable highlighting */
            currentJournal.DisableHighlight();
        } else if(currentJournal.GetAttachedHand() != null)
        {
            if (debug) Debug.Log("PlayerJournalManager: We are inspecting an item!");

            /* Our journal is probably in our other hand, so lets inspect this item if possible */
            inspecting = true;
            beforeInspecting = currentJournal.GetCurrentPagePairing();

            /* Open the inspector to this item */
            currentJournal.OpenInspector(item);
        }
    }

    protected override void OnItemRemovedFromHand(HandManager hand)
    {
        if (!isLocalPlayer || currentJournal == null)
            return;

        /* Was the item we were inspecting removed from our hand? */
        if (currentJournal.GetAttachedHand() != null && inspecting)
        {
            if (debug) Debug.Log("PlayerJournalManager: We have stopped inspecting an item.");

            /* Open the journal up to the page we were at before we started inspecting */
            currentJournal.OpenTo(beforeInspecting);
        }

        /* We are no longer inspecting an item */
        inspecting = false;
    }

    protected override bool OnItemPlacedInToolbelt(GameObject obj, PlayerToolbeltSlot slot, HandManager hand)
    {
        /* Is this our journal? */
        if (currentJournal == null || currentJournal.gameObject != obj)
            return false;

        /* Force close the journal */
        currentJournal.CloseJournal();

        /* We consumed this event */
        return true;
    }

    protected override bool OnInteractionBoxPressed(InteractionBox box, HandManager hand)
    {
        /* Is this a journal piece? */
        if (!(box is JournalPiece))
            return false;

        /* We have to have a journal */
        if (currentJournal == null)
        {
            Debug.LogError("PlayerJournalManager: Why do we not have a journal to manage?");
            return false;
        }

        /* Convert the interaction box to a journal piece */
        JournalPiece piece = (JournalPiece)box;

        /* Let the journal check to see if this event is for us */
        return currentJournal.OnInteractionBoxPressed(piece);
    }

    protected override void OnQuestAdded(Quest quest) { currentJournal.QuestAdded(quest); }
    protected override void OnQuestUpdated(Quest quest) { currentJournal.QuestUpdated(quest); }
    protected override void OnQuestCompleted(Quest quest) { currentJournal.QuestUpdated(quest); }
    protected override void OnQuestFailed(Quest quest) { currentJournal.QuestUpdated(quest); }
}
