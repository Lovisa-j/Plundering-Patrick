using System.Collections.Generic;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
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
    public static ObjectiveManager instance;

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
            if (mapObjectives[i].id == id)
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
        destination
    }

    [Header("Visual")]
    public string name;
    [Space(10)]
    [TextArea(1, 3)] public string description;

    [Header("Setup")]
    public int id;
    public TrackingStat trackedStat;
    public string[] validIds;
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
            default:
                break;
        }
        active = true;
    }

    public void CompleteOne(string completedId)
    {
        if (!active)
            return;

        bool contains = false;
        for (int i = 0; i < validIds.Length; i++)
        {
            if (completedId.Contains(validIds[i]))
            {
                contains = true;
                break;
            }
        }

        if (!contains)
            return;

        completions++;
        if (completions >= requiredCompletions)
        {
            completed = true;
            for (int i = 0; i < ObjectiveManager.instance.mapObjectives.Length; i++)
            {
                if (ObjectiveManager.instance.mapObjectives[i].id == nextObjectiveId)
                    ObjectiveManager.instance.mapObjectives[i].Initialize();
            }
            onComplete?.Invoke();

            active = false;
        }
    }
}

public class GameEvents : MonoBehaviour
{
    public static System.Action<string> onEntityDeath;
    public static System.Action<string> onInteraction;
    public static System.Action<string> onEnterArea;
}
