using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SojournSkyManager : NetworkBehaviour
{
    [Tooltip("Determines the time to transition between day cycles. Smaller is slower.")]
    [UnityEngine.Serialization.FormerlySerializedAs("TimeConstant")]
    public float transitionSpeed = 0.5f;

    public enum Cycle
    {
        Dawn,
        Day,
        Dusk,
        Night,
        None
    }

    /* The current cycle of the day */
    private Cycle cycle;

    /* The cycle that we're lerping to. This triggers the lerp on clients */
    [SyncVar(hook = "CycleTargetUpdated")]
    private Cycle targetCycle;

    /* Materials of respective times */
    public Material dayTime;
    public Material nightTime;
    public Material duskTime;
    public Material dawnTime;
    public Material skyBox;
    public Material ocean;

    /* Light object of scene */
    private GameObject directionalLight;

    /* Colors of respective times */
    public Color dayColor;
    public Color nightColor;
    public Color duskColor;
    public Color dawnColor;

    /* Ocean Colors */
    public Color oceanNightColor;
    private Color oceanDayColor;
    public Color oceanDawnColor;
    public Color oceanDuskColor;

    /* Timer to keep track of in-game time */
    private float updateTimer;
    /* Timer to keep track of the lerp between cycles */
    private float timeLerp;

    /* The time required for a full day cycle */
    public float maxMinutesDay;
    public float maxMinutesNight;
    public float maxMinutesDawn;
    public float maxMinutesDusk;

    /* The settings for the sky and ocean for the current cycle */
    private Color skyCurrent;
    private Color skyTarget;
    private Color oceanCurrent;
    private Color oceanTarget;
    private float maxMinutes;

    /* Whether or not to print debug messages for this class. */
    private static readonly bool debug = true;

    protected void Awake()
    {
        if (debug)
            Debug.Log("SojournSkyManager: Starting...");

        /* Instance the materials */
        dayTime = new Material(dayTime);
        nightTime = new Material(nightTime);
        duskTime = new Material(duskTime);
        dawnTime = new Material(dawnTime);
        skyBox = new Material(skyBox);
        ocean = new Material(ocean);

        /* Reset our state */
        timeLerp = 0.0f;
        updateTimer = 0.0f;
        targetCycle = cycle = Cycle.Day;
        oceanDayColor = ocean.GetColor("_ReflectionColor");

        /* Let's try to find the directional light */
        foreach (Light light in FindObjectsOfType<Light>())
        {
            /* Is this a directional light? */
            if (light.type == LightType.Directional)
            {
                /* We shouldn't already have a directional light */
                if (directionalLight != null)
                {
                    Debug.LogWarning("SojournSkyManager: There is more than one directional light!");
                    continue;
                }

                /* Save the directional light */
                directionalLight = light.gameObject;

                if (debug)
                    Debug.Log("SojournSkyManager: Directional light found.");
            }
        }

        /* Did we find a directional light? */
        if (directionalLight == null)
        {
            Debug.LogError("SojournSkyManager: Couldn't find a directional light!");
            return;
        }

        /* Update the sky and ocean settings */
        UpdateColors(cycle);
    }

    public override void OnStartClient()
    {
        /* If this is a client only, update the light settings to the target */
        if (!isServer && isClient)
        {
            if (debug)
                Debug.Log("SojournSkyManager: Started client only, starting with cycle=" + targetCycle);

            /* Update the sky colors, and reset the target */
            UpdateColors(targetCycle);
            targetCycle = Cycle.None;
        }
    }

    public override void OnStartServer()
    {
        /* Send the initial cycle to the game manager */
        SojournGameManager.gameManager.TimeCycleChanged(cycle);
    }

    /**
     * This is called on clients when the target cycle has been
     * updated. This signifies that we should start lerping from
     * one cycle to another.
     */
    private void CycleTargetUpdated(Cycle targetCycle)
    {
        /* Ignore this on local hosts */
        if (isServer)
            return;

        if (debug) Debug.Log("SojournSkyManager: Client target cycle updated: " + targetCycle);

        /* Update the target cycle */
        this.targetCycle = targetCycle;
    }

    private void UpdateColors(Cycle currentCycle)
    {
        /* Update the colors based off of the current state */
        switch (currentCycle)
        {
            case Cycle.Day:
                /* Going from day to dusk */
                skyCurrent = dayColor;
                skyTarget = duskColor;
                oceanCurrent = oceanDayColor;
                oceanTarget = oceanDuskColor;
                maxMinutes = maxMinutesDay;
                dayTime.SetFloat("_SkyBlend", 0);
                RenderSettings.skybox = dayTime;
                break;

            case Cycle.Dusk:
                /* Going from dusk to night */
                skyCurrent = duskColor;
                skyTarget = nightColor;
                oceanCurrent = oceanDuskColor;
                oceanTarget = oceanNightColor;
                maxMinutes = maxMinutesDusk;
                duskTime.SetFloat("_SkyBlend", 0);
                RenderSettings.skybox = duskTime;
                break;

            case Cycle.Night:
                /* Going from night to dawn */
                skyCurrent = nightColor;
                skyTarget = dawnColor;
                oceanCurrent = oceanNightColor;
                oceanTarget = oceanDawnColor;
                maxMinutes = maxMinutesNight;
                nightTime.SetFloat("_SkyBlend", 0);
                RenderSettings.skybox = nightTime;
                break;

            case Cycle.Dawn:
                /* Going from dawn to day */
                skyCurrent = dawnColor;
                skyTarget = dayColor;
                oceanCurrent = oceanDawnColor;
                oceanTarget = oceanDayColor;
                maxMinutes = maxMinutesDawn;
                dawnTime.SetFloat("_SkyBlend", 0);
                RenderSettings.skybox = dawnTime;
                break;
            default:
                Debug.LogError("SojournSkyManager: Unknown cycle: " + cycle);
                break;
        }

        Debug.Assert(directionalLight != null);
        directionalLight.GetComponent<Light>().color = skyCurrent;
        ocean.SetColor("_ReflectionColor", oceanCurrent);

        if (debug)
            Debug.Log("SojournSkyManager: Minutes to next cycle: " + maxMinutes);
    }

    private void ColorLerp()
    {
        /* Increase the lerp value */
        timeLerp = Mathf.Min(timeLerp + (Time.deltaTime * transitionSpeed), 1.0f);

        /* Lerp the directional light and the ocean color */
        directionalLight.GetComponent<Light>().color = Color.Lerp(skyCurrent, skyTarget, timeLerp);
        ocean.SetColor("_ReflectionColor", Color.Lerp(oceanCurrent, oceanTarget, timeLerp));

        /* Lerp the skybox materials */
        switch (cycle)
        {
            case Cycle.Day:
                dayTime.SetFloat("_SkyBlend", timeLerp);
                break;
            case Cycle.Dusk:
                duskTime.SetFloat("_SkyBlend", timeLerp);
                break;
            case Cycle.Night:
                nightTime.SetFloat("_SkyBlend", timeLerp);
                break;
            case Cycle.Dawn:
                dawnTime.SetFloat("_SkyBlend", timeLerp);
                break;
            default:
                Debug.LogError("SojournSkyManager: Unknown cycle: " + cycle);
                break;
        }
    }

    /**
     * Returns the cycle after the given cycle.
     */
    private Cycle NextCycle(Cycle cycle)
    {
        switch (cycle)
        {
            case Cycle.Day:
                return Cycle.Dusk;
            case Cycle.Dusk:
                return Cycle.Night;
            case Cycle.Night:
                return Cycle.Dawn;
            case Cycle.Dawn:
                return Cycle.Day;
            default:
                Debug.LogError("Unknown cycle: " + cycle);
                break;
        }

        return Cycle.None;
    }

    public void UpdateOceanMaterial(Renderer oceanRenderer)
    {
        /* Update the ocean material */
        oceanRenderer.material = ocean;
    }

    protected void Update()
    {
        if (directionalLight == null)
            return;

        if (!(isServer || isClient))
            return;

        /* Call the update function for the client or the server. Localhosts use server function */
        if (isServer)
            ServerUpdate();
        else ClientUpdate();
    }

    protected void OnDestroy() { }
    protected void OnApplicationQuit() { }

    private void InternalRevertMaterials()
    {

    }

    private void ServerUpdate()
    {
        /* Add the time passed to our overall time counter */
        updateTimer += Time.deltaTime;

        /* Did we go past the maximum amount of minutes for this cycle? */
        if (updateTimer > maxMinutes * 60.0f)
        {
            if (timeLerp == 0.0f && debug)
                Debug.Log("SojournSkyManager: Lerp has started.");

            /* Update target if needed */
            if (targetCycle == cycle)
                targetCycle = NextCycle(cycle);

            /* Progress the color lerp */
            ColorLerp();

            /* Has the lerp completed? */
            if (timeLerp >= 1)
            {
                /* Reset the update timer and lerp timer */
                updateTimer = 0;
                timeLerp = 0;

                /* Change to the next cycle */
                cycle = NextCycle(cycle);
                Debug.Assert(cycle == targetCycle);

                /* Let our game manager know that we have changed cycles */
                SojournGameManager.gameManager.TimeCycleChanged(cycle);

                if (debug) Debug.Log("SojournSkyManager: Cycle has been updated: " + cycle);

                /* Update the colors for the sky */
                UpdateColors(cycle);
            }
        }
    }

    private void ClientUpdate()
    {
        /* are we lerping? */
        if (targetCycle != Cycle.None)
        {
            /* Start the lerp */
            ColorLerp();

            /* Has the lerp completed? */
            if (timeLerp >= 1.0f)
            {
                /* Set our cycle to the target */
                cycle = targetCycle;
                /* Reset the lerp timer */
                timeLerp = 0.0f;
                /* Update our colors */
                UpdateColors(cycle);

                /* We have reached the target */
                targetCycle = Cycle.None;
            }
        }
    }
}
