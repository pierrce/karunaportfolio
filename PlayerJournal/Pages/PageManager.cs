using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageManager
{
    /**
     * A page pairing. This represents the left and
     * right pages that makes up a journal page view.
     */
    public class PagePairing
    {
        public PagePairing(JournalPage pageLeft, JournalPage pageRight)
        {
            this.pageLeft = pageLeft;
            this.pageRight = pageRight;
            id = null;
        }

        public PagePairing(JournalPage pageLeft, JournalPage pageRight, string id)
        {
            this.pageLeft = pageLeft;
            this.pageRight = pageRight;
            this.id = id;
        }

        public void CullPages()
        {
            pageLeft.CullPage();
            pageRight.CullPage();
        }

        public void SetState(JournalPage.JournalPageState state, float journalLerp)
        {
            pageLeft.SetState(state, journalLerp);
            pageRight.SetState(state, journalLerp);
        }
        public void SetState(JournalPage.JournalPageState state) { SetState(state); }

        public void Destroy()
        {
            GameObject.Destroy(pageLeft.gameObject);
            GameObject.Destroy(pageRight.gameObject);
        }

        public readonly JournalPage pageLeft;
        public readonly JournalPage pageRight;
        public readonly string id;
    }

    /* The list of meta pages at the beginning (item inspector, player stats, ect.)*/
    private List<PagePairing> metaList;
    /* Quests athat are in progress (Quest UUID is key) */
    private List<PagePairing> ipQuests;
    /* Quests that are completed (Quest UUID is key) */
    private List<PagePairing> cQuests;
    /* This is the list that contains all of our pairing lists */
    private List<List<PagePairing>> masterList;

    /* The item inspector pages */
    private PagePairing itemInspector;
    /* The players stats pages */
    private PagePairing playerStats;

    /* The index in our current list */
    private int currentIndex;
    /* Our current state */
    private List<PagePairing> currentList;

    /* The index we're trying to get to in our target state */
    private int targetIndex;
    /* The list that our target pair is in */
    private List<PagePairing> targetList;

    /* The journal that we're managing the pages of */
    private JournalManager journal;

    /* The timer that creates space in between pages flips */
    private float flipTimer;

    /* The value that the flip timer is reset to */
    private readonly float flip_timer_reset = 0.10f;

    private readonly bool debug = false;

    public PageManager(JournalManager journal, List<Quest> currentQuests, List<Quest> completedQuests)
    {
        /* Save the journal */
        this.journal = journal;

        /* Setup required lists */
        metaList = new List<PagePairing>();
        ipQuests = new List<PagePairing>();
        cQuests = new List<PagePairing>();

        /* Setup the master list */
        masterList = new List<List<PagePairing>>();
        masterList.Add(metaList);
        masterList.Add(ipQuests);
        masterList.Add(cQuests);

        /* Setup the item inspector */
        itemInspector = LoadPair("ItemInspectorLeftPage", "ItemInspectorRightPage");
        /* Setup the players stats */
        playerStats = LoadPair("PlayerStatsLeftPage", "PlayerStatsRightPage");

        /* Add these pairings to the meta list */
        metaList.Add(itemInspector);
        metaList.Add(playerStats);

        /* Add all of the in progress quests */
        foreach (Quest q in currentQuests)
            ipQuests.Add(CreateQuestPair(q));

        /* Add all of the completed quests */
        foreach (Quest q in completedQuests)
            cQuests.Add(CreateQuestPair(q));

        /* We start out just looking at the player stats */
        currentList = targetList = metaList;
        currentIndex = targetIndex = metaList.IndexOf(playerStats);
        /* We're ready to flip whenever */
        flipTimer = 0.0f;
    }

    /**
     * Open the player journal open to the given quest.
     */
    public bool OpenQuest(Quest quest)
    {
        if (debug) Debug.Log("PageManager: We are opening the journal to a quest: " + quest.quest_id);

        /* The page pairing we are hoping to find */
        PagePairing pair = null;

        /* Is this a quest in progress? */
        if((pair = GetQuestPagePair(ipQuests, quest.quest_id)) != null)
        {
            /* Set our target */
            if(!UpdateTarget(pair, ipQuests, false))
            {
                Debug.LogError("PageManager: Failed to find target!");
                return false;
            }

            /* We updated the target */
            return true;
        } else if((pair = GetQuestPagePair(cQuests, quest.quest_id)) != null)
        {
            /* Set our target */
            if (!UpdateTarget(pair, cQuests, false))
            {
                Debug.LogError("PageManager: Failed to find target!");
                return false;
            }

            /* We updated the target */
            return true;
        }

        if (debug) Debug.LogWarning("PageManager: We failed to find the page pairing that is responsible for this quest.");

        /* We didn't find this quest */
        return false;
    }

    /**
     * Opens the journal to the player stats page.
     */
    public bool OpenPlayerStats()
    {
        if (debug) Debug.Log("PageManager: We are opening the player stats.");

        /* Sets the target to the player stats */
        return UpdateTarget(playerStats);
    }

    /**
     * Opens the item inspector to inspect the given item.
     */
    public bool OpenItemInspector(SojournItem item)
    {
        if (debug) Debug.Log("PageManager: We are opening the item inspector to inspect: " + item.itemName);

        /* Setup the left page */
        ItemInspectorLeftPage leftPage = (ItemInspectorLeftPage)itemInspector.pageLeft;
        leftPage.InspectItem(item.GetItem());

        /* Setup the right page */
        ItemInspectorRightPage rightPage = (ItemInspectorRightPage)itemInspector.pageRight;
        rightPage.SetDescription("");

        /* Open to the item inspector */
        return UpdateTarget(itemInspector);
    }

    /**
     * Open up to a specific page pair in the journal.
     */
    public bool OpenTo(PagePairing pair)
    {
        if (debug) Debug.Log("PageManager: We are attempting to open this journal to: " + pair.id);

        /* Update the target */
        return UpdateTarget(pair);
    }

    /**
     * Update the target that the book should flip to. This is an optimization method
     * where you can supply a list in which to search for the page pairing. You can also
     * restrict the search to only this list. If restrict is set to false, if the search
     * fails then all lists will be searched.
     */
    private bool UpdateTarget(PagePairing pair, List<PagePairing> list, bool restrict)
    {
        /* Does this list contain the pair? */
        if(list.Contains(pair))
        {
            /* Set the target */
            targetList = list;
            targetIndex = list.IndexOf(pair);

            if (debug) Debug.Log("PageManager: We found the target page pairing L" + masterList.IndexOf(targetList) + ":I" + targetIndex);

            /* Target has been set */
            return true;
        }

        if (debug) Debug.LogWarning("PageManager: Failed to find page pairing in list!");

        /* We failed to find the page pairing in the given list */
        if (restrict)
            return false;

        if (debug) Debug.Log("PageManager: We are doing a master search.");

        /* Try searching through all of the lists */
        return UpdateTarget(pair);
    }

    /**
     * Update the target that the book is flipping to.
     */
    private bool UpdateTarget(PagePairing pair)
    {
        foreach(List<PagePairing> list in masterList)
        {
            /* Does this list contain this page pair? */
            if(list.Contains(pair))
            {
                /* Set the target */
                targetList = list;
                targetIndex = list.IndexOf(pair);

                if (debug) Debug.Log("PageManager: We found the target page pairing L" + masterList.IndexOf(targetList) + ":I" + targetIndex);

                /* We found the page pairing */
                return true;
            }
        }

        if (debug) Debug.LogWarning("PageManager: We couldn't find the given page pairing!");

        /* We didn't find this page pair */
        return false;
    }

    /**
     * Update the player's stats in the journal. This does not flip the journal
     * open to the stats page and only updates the page's information.
     */
    public void UpdateStats(string player_name, EntitySkills skills, EntityCombatManager combat)
    {
        /* Load the left page */
        PlayerStatsLeftPage leftPage = (PlayerStatsLeftPage)playerStats.pageLeft;

        /* Update all of the page progresses */
        leftPage.UpdatePlayerName(player_name);
        if (skills != null)
            leftPage.UpdatePlayerSkills(skills);

        /* Load the right page */
        PlayerStatsRightPage rightPage = (PlayerStatsRightPage)playerStats.pageRight;

        /* Update the status effects that are affecting the player */
        if (combat != null)
            rightPage.UpdateStatusEffects(combat);
    }

    /**
     * Called when a player updates a quest.
     */
    public void UpdateQuest(Quest quest)
    {
        /* The page pairing we are hoping to find */
        PagePairing pair;

        /* Is this quest in the in progress list? */
        if ((pair = GetQuestPagePair(ipQuests, quest.quest_id)) != null)
        {
            /* Is this quest now complete? */
            if (quest.complete)
            {
                if (debug) Debug.Log("PageManager: A quest has been completed.");

                /* Are we looking at the quest that's being updated? */
                bool lookingAt = (pair == GetCurrentPair());

                /* Remove the page paring from the in progress list */
                ipQuests.Remove(pair);
                /* Add page paring to the list of completed quests */
                cQuests.Remove(pair);

                /* Are we looking at the quest we're updating? */
                if (lookingAt)
                {
                    /* Change our target to look at the page in the new position */
                    if (!UpdateTarget(pair, cQuests, false))
                    {
                        Debug.LogError("PageManager: We failed to find the target page!");
                        return;
                    }
                }
            }

            /* Update the information for this quest */
            UpdateQuestInfo(pair, quest);
        } else if ((pair = GetQuestPagePair(cQuests, quest.quest_id)) != null)
        {
            Debug.LogError("PageManager: How did a completed quest get updated?");

            /* Just update the quest information */
            UpdateQuestInfo(pair, quest);
        } else
        {
            if (debug) Debug.Log("PageManager: A new quest has been added.");

            Debug.LogError("PageManager: We don't have a reference to this quest. You should call AddQuest instead.");

            /* Add this quest */
            AddQuest(quest);
        }
    }

    /**
     * Called when the player has started a new quest.
     */
    public void AddQuest(Quest quest)
    {
        /* Is this quest complete? */
        if (quest.complete)
        {
            if (debug) Debug.Log("PageManager: We have added a completed quest.");

            /* Add the completed quest */
            cQuests.Add(CreateQuestPair(quest));
        } else
        {
            if (debug) Debug.Log("PageManager: We have added an in progress quest.");

            /* Add the in progress quest */
            ipQuests.Add(CreateQuestPair(quest));
        }
    }

    /**
     * Flip left one page.
     */
    public bool FlipLeft()
    {
        /* Save our index in case we have to revert */
        int originalIndex = targetIndex;

        /* Decrement the target index */
        targetIndex--;

        /* Do we have to change lists? */
        if (targetIndex < 0)
        {
            /* Get the index of our target list */
            int targetListIndex = masterList.IndexOf(targetList);
            List<PagePairing> newTargetList = null;

            /* Are there any lists available to the left of us? */
            for(int x = targetListIndex - 1;x >= 0;x--)
            {
                /* Get a list from the master list */
                List<PagePairing> next = masterList[x];

                /* Does this list have anything in it? */
                if (next.Count > 0)
                {
                    /* This list will work */
                    newTargetList = next;
                    break;
                }
            }

            /* Did we find a valid list? */
            if(newTargetList == null)
            {
                targetIndex = originalIndex;
                return false;
            }

            /* Update our target */
            targetList = newTargetList;
            targetIndex = targetList.Count - 1;
        }

        if (debug) Debug.Log("PageManager: New target: {" + masterList.IndexOf(targetList) + ", " + targetIndex + "}");

        /* We will flip to a new page */
        return true;
    }

    /**
     * Flip right one page.
     */
    public bool FlipRight()
    {
        /* Save our index in case we have to revert */
        int originalIndex = targetIndex;

        /* Increment the target index */
        targetIndex++;

        /* Do we have to change lists? */
        if (targetIndex >= targetList.Count)
        {
            /* Get the index of our target list */
            int targetListIndex = masterList.IndexOf(targetList);
            List<PagePairing> newTargetList = null;

            /* Are there any lists available to the left of us? */
            for (int x = targetListIndex + 1; x < masterList.Count; x++)
            {
                /* Get a list from the master list */
                List<PagePairing> next = masterList[x];

                /* Does this list have anything in it? */
                if (next.Count > 0)
                {
                    /* This list will work */
                    newTargetList = next;
                    break;
                }
            }

            /* Did we find a valid list? */
            if (newTargetList == null)
            {
                targetIndex = originalIndex;
                return false;
            }

            /* Update our target */
            targetList = newTargetList;
            targetIndex = 0;
        }

        if (debug) Debug.Log("PageManager: New target: {" + masterList.IndexOf(targetList) + ", " + targetIndex + "}");

        /* We will flip to a new page */
        return true;
    }

    /**
     * Called when the journal starts opening.
     */
    public void JournalOpening(float journalLerp)
    {
        /* We always start at our target */
        currentList = targetList;
        currentIndex = targetIndex;

        /* Get our current pair */
        PagePairing pair = GetCurrentPair();
        /* Uncull and set the state of the pair */
        pair.SetState(JournalPage.JournalPageState.OpeningWithJournal, journalLerp);
    }

    /**
     * Called when the journal starts closing, we just cull
     * all of the pages.
     */
    public void JournalClosing()
    {
        /* Cull all of the pages */
        itemInspector.CullPages();
        playerStats.CullPages();
        foreach (PagePairing p in ipQuests)
            p.CullPages();
        foreach (PagePairing p in cQuests)
            p.CullPages();
    }

    private PagePairing LoadPair(string leftPage, string rightPage)
    {
        return LoadPair(leftPage, rightPage, null);
    }

    private PagePairing LoadPair(string leftPage, string rightPage, string id)
    {
        /* Create the page pairing */
        PagePairing pair = new PagePairing(LoadPage(leftPage), LoadPage(rightPage), id);

        /* Cull the pair */
        pair.CullPages();

        /* Update the pairing for the pages */
        pair.pageLeft.SetPagePairing(pair);
        pair.pageRight.SetPagePairing(pair);

        /* Return the new pair */
        return pair;
    }

    private JournalPage LoadPage(string prefab_name)
    {
        /* Load the page prefab */
        UnityEngine.Object prefab = Resources.Load(prefab_name);

        if (prefab == null)
            throw new System.Exception("This page doesn't exist: " + prefab_name);

        /* Instantiate the object */
        GameObject obj = (GameObject)GameObject.Instantiate(prefab, journal.transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = Vector3.one;

        /* Get the journal page component from the page */
        JournalPage page = obj.GetComponent<JournalPage>();
        Debug.Assert(page != null);

        /* Return the page */
        return page;
    }

    private PagePairing CreateQuestPair(Quest quest)
    {
        /* Create the page pair */
        PagePairing pair = LoadPair("QuestLeftPage", "QuestRightPage", quest.quest_id);

        /* Update the information for this quest */
        UpdateQuestInfo(pair, quest);

        /* Return the pair */
        return pair;
    }

    /**
     * Updates the visual information on the page pairing
     * to the information in the given quest 
     */
    private void UpdateQuestInfo(PagePairing pair, Quest quest)
    {
        /* Get the left page */
        QuestsLeftPage leftPage = (QuestsLeftPage)pair.pageLeft;

        /* Update the quest name and quest description */
        leftPage.SetQuestInformation(quest.name, quest.description, quest.complete);

        /* Load the right page */
        QuestsRightPage rightPage = (QuestsRightPage)pair.pageRight;
        rightPage.ClearObjectives();

        /* Add completed objectives, and active objectives */
        foreach (Objective o in quest.GetCompletedObjectives())
            rightPage.AddCompletedObjective(o.GetDescription());

        /* Add the current objectives */
        foreach (Objective o in quest.GetObjectivesInProgress())
            rightPage.AddObjective(o.GetDescription());
    }

    /**
     * Attept to try to find a page pairing that is displaying
     * the quest with the given id.
     */
    private PagePairing GetQuestPagePair(List<PagePairing> l, string id)
    {
        /* Attempt to find the page pairing */
        foreach (PagePairing pair in l)
            if (pair.id == id)
                return pair;

        /* We didn't find this quest page pair */
        return null;
    }

    /**
     * Returns the current page pairing.
     */
    public PagePairing GetCurrentPair()
    {
        return currentList[currentIndex];
    }

    /**
     * Get the target pages we're trying to get to.
     */
    public PagePairing GetTargetPair()
    {
        return targetList[targetIndex];
    }

    private void CurrentFlipLeft(List<PagePairing> nextList, int nextIndex)
    {
        /* Get the old pair */
        PagePairing oldPair = GetCurrentPair();

        /* Update the current stats */
        currentList = nextList;
        currentIndex = nextIndex;

        /* Get this page pairing */
        PagePairing newPair = GetCurrentPair();

        /* Set the states of the pages */
        newPair.pageLeft.SetState(JournalPage.JournalPageState.CreatedResting);
        newPair.pageRight.SetState(JournalPage.JournalPageState.CreatedFlippingToResting);

        /* Flip the old left page */
        oldPair.pageLeft.SetState(JournalPage.JournalPageState.RestToFlipped);

        /* Make the flipping sound */
        journal.PageSwipingSound();

        if (debug) Debug.Log("PageManager: We are flipping left.");
    }

    private void CurrentFlipRight(List<PagePairing> nextList, int nextIndex)
    {
        /* Get the old pair */
        PagePairing oldPair = GetCurrentPair();

        /* Update the current stats */
        currentList = nextList;
        currentIndex = nextIndex;

        /* Get this page pairing */
        PagePairing newPair = GetCurrentPair();

        /* Set the states of the pages */
        newPair.pageLeft.SetState(JournalPage.JournalPageState.CreatedFlippingToResting);
        newPair.pageRight.SetState(JournalPage.JournalPageState.CreatedResting);

        /* Flip the old right page */
        oldPair.pageRight.SetState(JournalPage.JournalPageState.RestToFlipped);

        /* Make the flipping sound */
        journal.PageSwipingSound();

        if (debug) Debug.Log("PageManager: We are flipping right.");
    }

    public void PagesUpdate()
    {
        /* Do we have pages to turn? */
        flipTimer -= Time.deltaTime;
        if(flipTimer < 0)
        {
            /* Do some assertions to make sure we're sane */
            Debug.Assert(targetList != null && currentList != null);
            Debug.Assert(targetIndex >= 0 && targetIndex < targetList.Count);
            Debug.Assert(currentIndex >= 0 && currentIndex < currentList.Count);

            /* Do we have to turn a page? */
            if(targetIndex != currentIndex
                || targetList != currentList)
            {
                /* Are we in the same list? */
                if(targetList == currentList)
                {
                    /* Do we have to flip left or right? */
                    if (targetIndex > currentIndex)
                        CurrentFlipRight(currentList, currentIndex + 1);
                    else CurrentFlipLeft(currentList, currentIndex - 1);
                } else
                {
                    /* Get the indexes of our lists */
                    int targetListIndex = masterList.IndexOf(targetList);
                    int currentListIndex = masterList.IndexOf(currentList);

                    List<PagePairing> nextList = currentList;
                    int nextIndex = currentIndex;

                    if(targetListIndex > currentListIndex)
                    {
                        /* We have to go right */
                        nextIndex++;

                        /* Did we go past the end of the list? */
                        if(nextIndex >= nextList.Count)
                        {
                            /* Move one list to the right */
                            nextList = masterList[currentListIndex + 1];
                            nextIndex = 0;
                        }

                        /* Flip right */
                        CurrentFlipRight(nextList, nextIndex);
                    } else
                    {
                        /* We have to go left */
                        nextIndex--;

                        /* Did we go past the beginning of the list? */
                        if (nextIndex < 0)
                        {
                            /* Move one list to the right */
                            nextList = masterList[currentListIndex - 1];
                            nextIndex = nextList.Count - 1;
                        }

                        /* Flip left */
                        CurrentFlipLeft(nextList, nextIndex);
                    }
                }

                /* Reset the flip timer */
                flipTimer = flip_timer_reset;
            } else flipTimer = -1;
        }
    }

    private string GetNameForList(List<PagePairing> list)
    {
        if (list == metaList)
            return "Meta List";
        if (list == ipQuests)
            return "In Progress Quest";
        if (list == cQuests)
            return "Completed Quests";

        return "<UNKOWN>";
    }
}
