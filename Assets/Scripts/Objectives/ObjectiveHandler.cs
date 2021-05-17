using System.Collections.Generic;
using UnityEngine;

public class ObjectiveHandler : MonoBehaviour
{
    public Objective[] mapObjectives;

    public List<Objective> ActiveObjectives
    {
        get
        {
            List<Objective> activeObjectivesList = new List<Objective>();
            for (int i = 0; i < mapObjectives.Length; i++)
            {
                if (mapObjectives[i].active)
                    activeObjectivesList.Add(mapObjectives[i]);
            }

            return activeObjectivesList;
        }
    }

    #region singleton
    public static ObjectiveHandler instance;

    void Awake()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }
    #endregion

    void Start()
    {
        for (int i = 0; i < ActiveObjectives.Count; i++)
        {
            ActiveObjectives[i].Initialize();
        }
    }

    public void ActivateObjective(int id)
    {
        for (int i = 0; i < mapObjectives.Length; i++)
        {
            if (mapObjectives[i].id == id && !mapObjectives[i].completed)
                mapObjectives[i].Initialize();
        }
    }
}

[System.Serializable]
public class Objective
{
    public enum TrackingStat
    {
        kill,
        interaction,
        destination,
        hit
    }

    [Header("Visual")]
    public string name;
    [Space(10)]
    [TextArea(1, 3)] public string description;

    [Header("Setup")]
    public int id;
    public TrackingStat trackedStat;
    public string[] validTargetIds;
    public string[] validPerformerIds;
    public bool active;

    [Header("Completion")]
    public int requiredCompletions = 1;
    public int nextObjectiveId;
    public UnityEngine.Events.UnityEvent onComplete;

    public bool completed { get; private set; }

    int completions;
    public int Completions
    {
        get
        {
            return completions;
        }
    }

    public void Initialize()
    {
        completed = false;
        completions = 0;
        switch (trackedStat)
        {
            case TrackingStat.kill:
                GameEvents.onEntityDeath += CompleteOne;
                break;
            case TrackingStat.interaction:
                GameEvents.onInteraction += CompleteOne;
                break;
            case TrackingStat.destination:
                GameEvents.onEnterArea += CompleteOne;
                break;
            case TrackingStat.hit:
                GameEvents.onEntityHit += CompleteOne;
                break;
            default:
                break;
        }
        active = true;
    }

    public void CompleteOne(string completedId, string completedById)
    {
        if (!active || !IdsAreValid(completedId, completedById))
            return;

        completions++;
        if (completions >= requiredCompletions)
        {
            completed = true;
            for (int i = 0; i < ObjectiveHandler.instance.mapObjectives.Length; i++)
            {
                if (ObjectiveHandler.instance.mapObjectives[i].id == nextObjectiveId)
                    ObjectiveHandler.instance.mapObjectives[i].Initialize();
            }
            onComplete?.Invoke();

            active = false;
        }
    }

    bool IdsAreValid(string completedId, string completedById)
    {
        bool contains = false;
        for (int i = 0; i < validTargetIds.Length; i++)
        {
            if (completedId.Contains(validTargetIds[i]))
            {
                contains = true;
                break;
            }
        }

        if (!contains)
            return false;

        contains = false;
        for (int i = 0; i < validPerformerIds.Length; i++)
        {
            if (completedById == "")
            {
                if (validPerformerIds[i] == "")
                {
                    contains = true;
                    break;
                }
            }
            else if (completedById.Contains(validPerformerIds[i]))
            {
                contains = true;
                break;
            }
        }

        if (!contains)
            return false;

        return true;
    }
}

public class GameEvents : MonoBehaviour
{
    public static System.Action<string, string> onEntityDeath;
    public static System.Action<string, string> onInteraction;
    public static System.Action<string, string> onEnterArea;
    public static System.Action<string, string> onEntityHit;
}
