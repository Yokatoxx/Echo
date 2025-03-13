using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using EPOOutline;

public class OutlineParam : MonoBehaviour
{


    public float DilateMin = 1.0f;
    public float DilateMax = 2.0f;

    public float BlurMin = 0.5f;
    public float BlurMax = 1.5f;

    public float PulseDuration = 1.0f;
    public float PulseSpeed = 1.0f;

    public float PulseCooldown = 2.0f;

    private Outlinable outlinable;
    private bool canPulse = true;

    private void Awake()
    {
        outlinable = GetComponent<Outlinable>();

        if (outlinable != null)
        {
            outlinable.FrontParameters.DilateShift = 0f;
            outlinable.FrontParameters.BlurShift = 0f;
        }

    }


    public void PulseOutline()
    {
        if (outlinable != null && canPulse)
        {
            canPulse = false;

            float targetDilate = Random.Range(DilateMin, DilateMax);
            float targetBlur = Random.Range(BlurMin, BlurMax);

            float adjustedDuration = PulseDuration / PulseSpeed;

            //Tout un bordel pour faire un effet de pulse (voir la doc de DOTween)
            DOTween.To(() => outlinable.FrontParameters.DilateShift, x => outlinable.FrontParameters.DilateShift = x, targetDilate, adjustedDuration)
                   .SetEase(Ease.Linear)
                   .SetLoops(2, LoopType.Yoyo)
                   .OnComplete(() =>
                   {
                       outlinable.FrontParameters.DilateShift = 0f;
                   });

            DOTween.To(() => outlinable.FrontParameters.BlurShift, x => outlinable.FrontParameters.BlurShift = x, targetBlur, adjustedDuration)
                   .SetEase(Ease.Linear)
                   .SetLoops(2, LoopType.Yoyo)
                   .OnComplete(() =>
                   {
                       outlinable.FrontParameters.BlurShift = 0f;
                   });

            StartCoroutine(CooldownCoroutine());
        }
    }

    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(PulseCooldown);
        canPulse = true;
    }
}
