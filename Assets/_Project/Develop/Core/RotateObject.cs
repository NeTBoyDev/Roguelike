using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float RotateTime;
    void Start()
    {
        transform.DORotate(new Vector3(0, transform.rotation.eulerAngles.y + 180, 0), 10)
            .SetRelative()
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

}
