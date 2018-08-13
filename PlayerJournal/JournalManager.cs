using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class JournalManager : SojournItem
{
    [Header("Journal Properties")]
    [Tooltip("The left part of the journal")]
    public JournalPiece journalLeft;
    [Tooltip("The right part of the journal")]
    public JournalPiece journalRight;

    [Tooltip("The sound of pages swiping")]
    public AudioClip swipingClip;

    /* The sound player for this player journal */
    private SoundPlayer soundPlayer;

    private readonly float[] swipingStartTimes = { 0.0f, 1.1f, 2.0f, 3.2f };
    private readonly float swipeTime = 0.7f;

#if UNITY_EDITOR
    [Header("Journal Debugging")]
    public bool initializeJournal;
    public bool openJournal;
    public bool closeJournal;
    public bool flipRight;
    public bool flipLeft;
    public GameObject inspect = null;
    public bool highlight;
    public bool unhighlight;
#endif

    private struct JournalState
    {
        public JournalState(bool open)
        {
            this.open = open;
        }

        public static JournalState JournalOpen() { return new JournalState(true); }
        public static JournalState JournalClosed() { return new JournalState(false); }

        /* Whether or not the Journal is open */
        public readonly bool open;
    }

    [SyncVar(hook = "JournalStateUpdated")]
    private JournalState state;

    /* The object that helps manage all of the journal pages */
    private PageManager pages;

    /* The highlighter for the book */
    private JournalHighlighter highlighter;

    /* Whether or not the journal is opening or closing */
    private bool journalUpdating;

    /* Whether or not to print debug for this class */
    private static readonly bool debug = false;

    protected override void ItemAwake()
    {
        /* Get the sound player for the journal */
        soundPlayer = GetComponent<SoundPlayer>();
        if (soundPlayer == null)
            soundPlayer = gameObject.AddComponent<SoundPlayer>();

        /* Get the highlighter for the journal */
        highlighter = GetComponent<JournalHighlighter>();
        Debug.Assert(highlighter != null);

        /* We must have both of the parts of the book */
        if (journalLeft == null)
            throw new System.Exception("Journal left is null!");
        if (journalLeft == null)
            throw new System.Exception("Journal right is null!");
    }

    public void SetupPageManager(string player_name, PlayerQuestManager quests, EntitySkills skills, EntityCombatManager combat)
    {
        if (pages != null)
            return;

        /* Setup the page manager */
        pages = new PageManager(this, quests.GetCurrentQuests(), quests.GetCompletedQuests());
        /* Update player name */
        pages.UpdateStats(player_name, skills, combat);

        if (debug) Debug.Log("JournalManager: Journal has been setup.");
    }

    protected override void ItemOnStartServer()
    {
        /* Start the journal closed */
        CloseJournal();
    }

    protected override void ItemOnStartClient()
    {
        /* Go to the correct position */
        journalUpdating = true;
    }

    /**
     * Called when the journal's state changes.
     */
    private void JournalStateUpdated(JournalState state)
    {
        if (hasAuthority && isClient)
            return;

        /* Save the new state */
        this.state = state;
        /* Have our local journal animate to this new state */
        journalUpdating = true;

        if (debug) Debug.Log("JournalManager: Received update for client.");
    }

    [Command]
    private void CmdChangeJournalState(JournalState state)
    {
        /* Update our local state (will trigger hooks on clients) */
        this.state = state;
        journalUpdating = true;
    }

    public void OpenJournal()
    {
        /* Is this our journal? */
        if(!hasAuthority || !isClient)
        {
            if (debug) Debug.LogError("JournalManager: We don't manager this journal!");
            return;
        }

        /* Is the journal already open? */
        if(IsOpen())
        {
            if (debug) Debug.LogWarning("JournalManager: The journal is already open!");
            return;
        }

        /* Let our page manager know that the journal is opening */
        pages.JournalOpening(journalRight.GetLerpValue());

        /* Open the journal */
        state = JournalState.JournalOpen();
        journalUpdating = true;

        /* Change the journal state on the server */
        if(isClient && !isServer)
            CmdChangeJournalState(state);

        if (debug) Debug.LogWarning("JournalManager: The journal is opening.");
    }

    public void OpenPlayerStats()
    {
        /* Is this our journal? */
        if (!hasAuthority || !isClient)
        {
            if (debug) Debug.LogError("JournalManager: We don't manager this journal!");
            return;
        }

        /* Are we opening or closing the journal? */
        if (IsBusy() || IsClosed())
        {
            if (debug) Debug.LogError("JournalManager: The journal is busy!");
            return;
        }

        /* Open the player stats */
        if (!pages.OpenPlayerStats())
            Debug.LogError("Failed to open player stats!");
    }

    public void OpenQuest(Quest quest)
    {
        /* Is this our journal? */
        if (!hasAuthority || !isClient)
        {
            if (debug) Debug.LogError("JournalManager: We don't manager this journal!");
            return;
        }

        /* Are we opening or closing the journal? */
        if (IsBusy() || IsClosed())
        {
            if (debug) Debug.LogError("JournalManager: The journal is busy!");
            return;
        }

        if (!pages.OpenQuest(quest))
            Debug.LogError("JournalManager: The journal doesn't have this quest: " + quest);
    }

    public void OpenInspector(SojournItem item)
    {
        /* Is this our journal? */
        if (!hasAuthority || !isClient)
        {
            if (debug) Debug.LogError("JournalManager: We don't manager this journal!");
            return;
        }

        /* Are we opening or closing the journal? */
        if (IsBusy() || IsClosed())
        {
            if (debug) Debug.LogError("JournalManager: The journal is busy!");
            return;
        }

        if (!pages.OpenItemInspector(item))
            Debug.LogError("JournalManager: We cannot inspect this item: " + item.name);
    }

    /**
     * Opens the journal to a specific page pair. Returns true on success.
     */
    public bool OpenTo(PageManager.PagePairing pagePair)
    {
        /* Open the page manager open to these pages */
        return pages.OpenTo(pagePair);
    }

    /**
     * Close the player journal. This can be called on a client
     * or on the server.
     */
    public void CloseJournal()
    {
        /* Update our state */
        state = JournalState.JournalClosed();
        journalUpdating = true;

        /* Let the page manager know that the journal is closing */
        if(pages != null)
            pages.JournalClosing();

        /* Should we notify the server? */
        if (isClient && !isServer)
            CmdChangeJournalState(state);
    }

    public void UpdateStats(string player_name, EntitySkills skills, EntityCombatManager combat)
    {
        /* Update the stats of our player skills page */
        pages.UpdateStats(player_name, skills, combat);
    }

    public void QuestAdded(Quest quest)
    {
        if (debug) Debug.Log("JournalManager: A quest has been added to the journal: " + quest.name);

        /* Add this quest to the player journal */
        pages.AddQuest(quest);
    }

    public void QuestUpdated(Quest quest)
    {
        if (debug) Debug.Log("JournalManager: A quest has been updated in the journal: " + quest.name);

        /* Update the quest page for this quest. */
        pages.UpdateQuest(quest);
    }

    public bool OnInteractionBoxPressed(JournalPiece piece)
    {
        /* Was the left or right page clicked on? */
        if (piece == journalLeft)
            return pages.FlipLeft();
        else if (piece == journalRight)
            return pages.FlipRight();

        /* We don't care about this for whatever reason */
        return false;
    }

    public bool IsBusy()
    {
        if (journalUpdating)
            return true;
        return false;
    }

    public void PageSwipingSound()
    {
        /* Get an audio source */
        AudioSource source = soundPlayer.NextAudioSource();

        int n = Random.Range(0, swipingStartTimes.Length);
        AudioClip sub = MakeSubclip(swipingClip, swipingStartTimes[n], swipingStartTimes[n] + swipeTime);
        source.clip = sub;
        source.time = 0.0f;
        source.loop = false;
        source.Play();
    }

    /**
     * Returns the current page pairing that is being displayed.
     */
    public PageManager.PagePairing GetCurrentPagePairing()
    {
        return pages.GetCurrentPair();
    }

    /**
     * Creates a sub clip from an audio clip based off of the start time
     * and the stop time. The new clip will have the same frequency as
     * the original.
     */
    private AudioClip MakeSubclip(AudioClip clip, float start, float stop)
    {
        /* Create a new audio clip */
        int frequency = clip.frequency;
        float timeLength = stop - start;
        int samplesLength = (int)(frequency * timeLength) * clip.channels;
        AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, 1, frequency, false);

        /* Create a temporary buffer for the samples */
        float[] data = new float[samplesLength];
        /* Get the data from the original clip */
        clip.GetData(data, (int)(frequency * start));
        /* Transfer the data to the new clip */
        newClip.SetData(data, 0);

        /* Return the sub clip */
        return newClip;
    }

    protected override void ItemUpdate()
    {
        /* Should we update the journal pieces? */
        if (journalUpdating)
        {
            /* Assume we're done opening or closing the journal */
            bool done = true;

            /* Update both pages */
            done &= journalRight.JournalUpdate(state.open);
            done &= journalLeft.JournalUpdate(state.open);

            /* Are we actually done? */
            if (done)
            {
                if (debug) Debug.Log("JournalManager: The Journal has finished opening or closing.");
                journalUpdating = false;
            }
        }

        /* Update our page manager */
        if(pages != null)
            pages.PagesUpdate();

#if UNITY_EDITOR
        if (pages != null)
            initializeJournal = true;
        if(initializeJournal && pages == null)
        {
            //Quest ip1 = new Quest("In progress 1", "This is the first in progress quest.");
            //ip1.quest_id = System.Guid.NewGuid().ToString();
            //ip1.CreateObjectiveGroup().AddKillingObjective("goblin", 10, false, -1.0f, 0);

            //Quest ip2 = new Quest("In progress 2", "This is the second in progress quest.");
            //ip2.quest_id = System.Guid.NewGuid().ToString();
            //ip2.CreateObjectiveGroup().AddGatheringObjective("aloe", 100, false, -1.0f, 0);

            //List<Quest> ipq = new List<Quest>();
            //ipq.Add(ip1);
            //ipq.Add(ip2);

            //Quest c1 = new Quest("Complete quest 1", "This is the first complete quest.");
            //ip2.quest_id = System.Guid.NewGuid().ToString();
            //ip2.complete = true;
            //ip2.CreateObjectiveGroup().AddGatheringObjective("aloe", 100, false, -1.0f, 0);

            //List<Quest> cq = new List<Quest>();
            //cq.Add(c1);

            //pages = new PageManager(this, ipq, cq);

            //PlayerSkills skills = new PlayerSkills();
            //skills.exp_alchemy += 100000;
            //skills.exp_defense += 10000;
            //skills.exp_mining += 1000;
            //skills.exp_health += 100;
            //skills.exp_cooking += 1;

            //pages.UpdateStats("Sojourn Player", skills, null);

            initializeJournal = false;
        }

        if(openJournal)
        {
            OpenJournal();
            openJournal = false;
        }

        if(closeJournal)
        {
            CloseJournal();
            closeJournal = false;
        }

        if(flipLeft)
        {
            pages.FlipLeft();
            flipLeft = false;
        }

        if (flipRight)
        {
            pages.FlipRight();
            flipRight = false;
        }

        if(inspect != null)
        {
            SojournItem item = inspect.GetComponent<SojournItem>();
            if(item != null)
                OpenInspector(item);
            inspect = null;
        }

        if(highlight)
        {
            EnableHighlight();
            highlight = false;
        }

        if (unhighlight)
        {
            DisableHighlight();
            unhighlight = false;
        }
#endif
    }

    public bool IsOpen() { return !journalUpdating && state.open; }
    public bool IsClosed() { return !journalUpdating && !state.open; }
    public void EnableHighlight() { highlighter.EnableHighlighting(); }
    public void DisableHighlight() { highlighter.DisableHighlighting(); }
}
