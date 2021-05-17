using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Identification))]
public class DestinationObjective : MonoBehaviour
{
    public string[] testForIds;

    void OnTriggerEnter(Collider other)
    {
        Identification identity;
        Transform testTrans = other.transform;
        while (!testTrans.GetComponent<Identification>())
        {
            if (testTrans.parent == null)
                break;

            testTrans = testTrans.parent;
            if (testTrans == other.transform.root)
                break;
        }

        identity = testTrans.GetComponent<Identification>();
        if (identity == null)
            return;

        for (int i = 0; i < testForIds.Length; i++)
        {
            if (identity.id.Contains(testForIds[i]))
            {
                GameEvents.onEnterArea.Invoke(GetComponent<Identification>().id, identity.id);
                break;
            }
        }
    }
}
