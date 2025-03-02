using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ScaleObject : MonoBehaviour
{
    [SerializeField] private float interval;
    
    void Start()
    {
        DOTween.Sequence(transform.DOScale(new Vector3(1.5f, 1.5f, 1.5f), interval/2).SetEase(Ease.OutBack))
            .Append(transform.DOScale(new Vector3(1f, 1f, 1f), interval).SetEase(Ease.Linear))
            .SetLoops(-1, LoopType.Restart);
    }
}
