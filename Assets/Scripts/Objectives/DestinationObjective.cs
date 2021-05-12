using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Identification))]
public class DestinationObjective : MonoBehaviour
{
    public string testForId;

    void OnTriggerEnter(Collider other)
    {
        Identification identity = other.GetComponent<Identification>();
        if (identity == null)
            return;

        if (identity.id.Contains(testForId))
            GameEvents.onEnterArea.Invoke(GetComponent<Identification>().id);
    }
}
