using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnviroEcho : MonoBehaviour
{
    [SerializeField] private bool isWalkable = false;
    [SerializeField] private Material walkableMaterial;
    [SerializeField] private Material unwalkableMaterial;
    [SerializeField] private float walkableDuration = 5f;
    [SerializeField] private float blinkDuration = 2f;
    [SerializeField] private int blinkCount = 5;

    private Collider myCollider;
    private Renderer myRenderer;
    private Coroutine walkableTimerCoroutine;

    void Start()
    {
        myCollider = GetComponent<Collider>();
        myRenderer = GetComponent<Renderer>();
        SetWalkable(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scanner"))
        {
            if (walkableTimerCoroutine != null)
            {
                StopCoroutine(walkableTimerCoroutine);
            }

            SetWalkable(true);
            walkableTimerCoroutine = StartCoroutine(WalkableTimer());
        }
    }

    private IEnumerator WalkableTimer()
    {
        // Période où le pont reste walkable
        yield return new WaitForSeconds(walkableDuration - blinkDuration);

        // Calcul du temps entre chaque clignotement
        float blinkInterval = blinkDuration / blinkCount / 2;

        // Séquence de clignotement
        for (int i = 0; i < blinkCount; i++)
        {
            if (myRenderer != null)
            {
                myRenderer.material = unwalkableMaterial;
                yield return new WaitForSeconds(blinkInterval);
                myRenderer.material = walkableMaterial;
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        SetWalkable(false);
    }

    private void SetWalkable(bool walkable)
    {
        isWalkable = walkable;

        if (myCollider != null)
        {
            myCollider.isTrigger = !walkable;
        }

        if (myRenderer != null)
        {
            myRenderer.material = isWalkable ? walkableMaterial : unwalkableMaterial;
        }

    }
}
