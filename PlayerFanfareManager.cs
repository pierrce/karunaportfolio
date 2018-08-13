using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerFanfareManager : InteractionManager {

	/* Audio Source attached to the player */
    private AudioSource source;
	 /* Levelup Audio Clip */
    public AudioClip levelup;
	/* Quest Add Audio Clip */
	public AudioClip questadded;
	/* Quest Complete Audio Clip */
	public AudioClip questcompleted;
	/* For debugging only */
	private bool debug = false;
	/* Volume for all played clips */
	public float volume = 1.0f;

	/* This is called after the InteractionManagers Awake function */
	protected override void AfterAwake ()
	{
		/* Get the audio sources attached to the player character */
		if (gameObject.GetComponent<AudioSource> () == null)
			Debug.LogError ("PlayerFanfareManager: AudioSource doesn't exist on VRPlayer");
		else
			source = gameObject.GetComponent<AudioSource> ();

		if (debug) {

			if (levelup == null)
				Debug.LogError ("PlayerFanfareManager: Levelup audio clip does not exist.");

			if (questadded == null)
				Debug.LogError ("PlayerFanfareManager: Questadded audio clip does not exist.");

			if (questcompleted == null)
				Debug.LogError ("PlayerFanfareManager: Questcompleted audio clip does not exist.");

		}

		source.volume = volume;
	}

	/* When the player gains a level, play the level-up fanfare. */
	protected override void OnLevelGained(EntitySkills.Skill skill, EntitySkills skills)
    {
		PlayClip (levelup);
    }

	/* When a player gets a quest, play this clip. */
	protected override void OnQuestAdded(Quest quest)
	{
		PlayClip (questadded);
	}

	/* When a player completes a quest, play this clip */
	protected override void OnQuestCompleted(Quest quest)
	{
		PlayClip (questcompleted);
	}

	/* Instead of rewrite the same four lines in every function I made this */
	private void PlayClip(AudioClip clip)
	{
		source.Stop ();
		source.clip = clip;
		source.loop = false;
		source.Play ();
	}
}
