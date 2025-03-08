using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public enum MoveType
{
    Forward,
    Circle,
    OrbitTarget
}
public class TweenMover : MonoBehaviour
{
    [field: SerializeField] public MoveType MoveType { get; private set; } = MoveType.Forward;

    [SerializeField, MinValue(0)] private float moveDuration = 2f;

    [SerializeField, MinValue(0), ShowIf(nameof(MoveType), MoveType.Forward)] private float forwardDistance = 5f;
    [ShowIf(nameof(MoveType), MoveType.Forward)] public UnityEvent OnAnimationEnded;

    [SerializeField, ShowIf(nameof(MoveType), MoveType.Circle)] private float circleRadius = 3f;
    [SerializeField, ShowIf(nameof(MoveType), MoveType.Circle)] private float circleSpeed = 1f;

    [SerializeField, ShowIf(nameof(MoveType), MoveType.OrbitTarget)] private Transform orbitTarget;
    [SerializeField, ShowIf(nameof(MoveType), MoveType.OrbitTarget)] private float orbitRadius = 5f;
    [SerializeField, ShowIf(nameof(MoveType), MoveType.OrbitTarget)] private int orbitTimes = -1;

    [Header("Curve Influence")]
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f); //По умолчанию линейная (без изменений)
    [SerializeField] private float heightScale = 1f; //Масштаб влияния кривой на высоту

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private void Awake()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    [Button, EnableIf(nameof(IsPlaying))]
    public void StartMove()
    {
        DOTween.Kill(transform);

        switch (MoveType)
        {
            case MoveType.Forward:
                MoveForward();
                break;

            case MoveType.Circle:
                MoveCircle();
                break;

            case MoveType.OrbitTarget:
                MoveOrbitTarget();
                break;
        }
    }
    [Button, EnableIf(nameof(IsPlaying))]
    public void StopMove()
    {
        DOTween.KillAll(transform);
        
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
    }

    private bool IsPlaying() => Application.isPlaying;

    #region Move Algorithms

    public void SetMoveType(MoveType moveType) => MoveType = moveType;
    private void MoveForward()
    {
        Vector3 forwardDirection = transform.forward * forwardDistance;
        Vector3 targetPosition = _initialPosition + forwardDirection;
        Debug.Log($"Initial position: {_initialPosition}, Target position: {targetPosition}");

        float progress = 0f;
        transform.position = _initialPosition;

        DOTween.To(() => progress, x => progress = x, 1f, moveDuration)
            .SetEase(Ease.InOutSine)
            .OnUpdate(() => {
                Vector3 newPosition = Vector3.Lerp(_initialPosition, targetPosition, progress);

                float curveValue = heightCurve.Evaluate(progress);
                float heightOffset = curveValue * heightScale;

                newPosition.y = newPosition.y + heightOffset;

                transform.position = newPosition;
            })
            .OnComplete(() => OnAnimationEnded?.Invoke());
    }

    private void MoveCircle()
    {
        float angle = 0f;
        transform.position = _initialPosition + new Vector3(circleRadius, 0, 0);

        DOTween.To(() => angle, x => angle = x, 360f, circleSpeed)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                float progress = angle / 360f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 newPosition = _initialPosition + new Vector3(
                    Mathf.Cos(rad) * circleRadius,
                    0,
                    Mathf.Sin(rad) * circleRadius
                );

                float heightOffset = heightCurve.Evaluate(progress) * heightScale;
                newPosition.y += heightOffset;
                transform.position = newPosition;
                transform.LookAt(_initialPosition);
            });
    }

    private void MoveOrbitTarget()
    {
        if (orbitTarget == null)
        {
            Debug.LogWarning("OrbitTarget: Цель не задана!");
            return;
        }

        float angle = 0f;
        transform.position = orbitTarget.position + new Vector3(orbitRadius, 0, 0);

        DOTween.To(() => angle, x => angle = x, 360f, moveDuration)
            .SetLoops(orbitTimes, LoopType.Restart)
            .OnUpdate(() =>
            {
                float progress = angle / 360f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 newPosition = orbitTarget.position + new Vector3(
                    Mathf.Cos(rad) * orbitRadius,
                    0,
                    Mathf.Sin(rad) * orbitRadius
                );

                float heightOffset = heightCurve.Evaluate(progress) * heightScale;
                newPosition.y += heightOffset;

                transform.position = newPosition;
                transform.LookAt(orbitTarget.position);
            });
    }
    #endregion
}
