using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tools : MonoBehaviour
{
    public static void SoundFromPosition(Vector3 fromPosition, float soundDistance)
    {
        Collider[] colliders = Physics.OverlapSphere(fromPosition, soundDistance);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].GetComponent<EnemyAi>())
                colliders[i].GetComponent<EnemyAi>().AlertToPosition(fromPosition);
        }
    }
}
