using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScannerDetector : MonoBehaviour
{
    private MovingObject movingObject;
    private OutlineParam outlineParam;

    void Start()
    {
        movingObject = GetComponent<MovingObject>();
        outlineParam = GetComponent<OutlineParam>();

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Scanner") && movingObject != null)
        {
            movingObject.StopMovement();

            if (outlineParam != null)
            {
                outlineParam.PulseOutline();
                float totalDuration = outlineParam.PulseDuration + outlineParam.PulseCooldown;
                StartCoroutine(ResumeMovementAfterPulse(totalDuration));
            }
        }
    }

    private IEnumerator ResumeMovementAfterPulse(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (movingObject != null)
        {
            movingObject.ResumeMovement();
        }
    }
}
