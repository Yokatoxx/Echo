using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolRoute : MonoBehaviour
{


    public Transform[] patrolPoints;

    private void Awake()
    {
        patrolPoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).CompareTag("PatrolPoint"))
            {
                patrolPoints[i] = transform.GetChild(i);
            }

        }
    }
}
