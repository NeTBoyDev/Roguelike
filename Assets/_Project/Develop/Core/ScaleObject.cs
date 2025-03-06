using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ScaleObject : MonoBehaviour
{
    [SerializeField] private float interval;
    
    void Start()
    {
        DOTween.Sequence()
            .Append(DOTween.To(() => transform.localScale, x => transform.localScale = x, new Vector3(1.1f, 1.1f, 1.1f), interval/2).SetEase(Ease.OutBack))
            .Append(DOTween.To(() => transform.localScale, x => transform.localScale = x, new Vector3(1f, 1f, 1f), interval).SetEase(Ease.Linear))
            .SetLoops(-1, LoopType.Restart);
    }
}
