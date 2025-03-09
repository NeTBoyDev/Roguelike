using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using System;
using UnityEngine;
using UnityEngine.Events;

public enum MoveType
{
    Forward,
    Circle,
    OrbitTarget,
    CustomDirection
}

public class TweenMover : MonoBehaviour
{
    [field: SerializeField] public MoveType MoveType { get; private set; } = MoveType.Forward;

    [SerializeField, MinValue(0)] private float moveDuration = 2f;
    [SerializeField, MinValue(0)] private float delayBeforeStart = 0f;

    [SerializeField, MinValue(0), ShowIf(nameof(MoveType), MoveType.Forward)] private float forwardDistance = 5f;
    [SerializeField, ShowIf(nameof(MoveType), MoveType.CustomDirection)] private Vector3 customDirection = Vector3.forward;
    [SerializeField] private bool useLocalSpace = true;
    [SerializeField] private Transform target; //������� ������ ��� ��������
    public Action<Collider> OnAnimationEnded;

    [SerializeField, ShowIf(nameof(MoveType), MoveType.Circle)] private float circleRadius = 3f;
    [SerializeField, ShowIf(nameof(MoveType), MoveType.Circle)] private float circleSpeed = 1f;

    [SerializeField, ShowIf(nameof(MoveType), MoveType.OrbitTarget)] private Transform orbitTarget;
    [SerializeField, ShowIf(nameof(MoveType), MoveType.OrbitTarget)] private float orbitRadius = 5f;
    [SerializeField, ShowIf(nameof(MoveType), MoveType.OrbitTarget)] private int orbitTimes = -1;

    [Header("Curve Influence")]
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    [SerializeField] private float heightScale = 1f;

    [SerializeField] private bool _autoReset = true;
    [SerializeField] private UnityEvent AnimationEnded;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private Tween _moveTween;
    [SerializeField, ReadOnly] private bool isAnimating;
    public bool IsAnimationFinished { get; private set; }
    private Collider _targerColldier;

    private Transform TargetTransform => target != null ? target : transform;

    private void Awake()
    {
        if (useLocalSpace && TargetTransform.parent != null)
        {
            originalPosition = TargetTransform.localPosition;
            originalRotation = TargetTransform.localRotation;
        }
        else
        {
            originalPosition = TargetTransform.position;
            originalRotation = TargetTransform.rotation;
        }
        initialPosition = originalPosition;
        initialRotation = originalRotation;
        IsAnimationFinished = false;
        isAnimating = false;

        if(_autoReset)
            OnAnimationEnded += (a) => StopMove(1);
    }

    private void OnDestroy() => OnAnimationEnded = null;

    [Button, EnableIf(nameof(CanStartAnimation))]
    public void StartMove()
    {
        if (isAnimating || IsAnimationFinished)
        {
            Debug.LogWarning("Animation is already running or finished!");
            return;
        }

        DOTween.Kill(TargetTransform);
        ResetToOriginalPosition();
        initialPosition = useLocalSpace && TargetTransform.parent != null ? TargetTransform.localPosition : TargetTransform.position;
        initialRotation = useLocalSpace && TargetTransform.parent != null ? TargetTransform.localRotation : TargetTransform.rotation;

        IsAnimationFinished = false;
        isAnimating = true;

        DOVirtual.DelayedCall(delayBeforeStart, () =>
        {
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
                case MoveType.CustomDirection:
                    MoveCustomDirection();
                    break;
            }
        });
    }

    [Button, EnableIf(nameof(IsPlaying))]
    public void PlayWithoutReset()
    {
        if (isAnimating || IsAnimationFinished)
        {
            Debug.LogWarning("Animation is already running or finished!");
            return;
        }

        DOTween.Kill(TargetTransform);
        initialPosition = useLocalSpace && TargetTransform.parent != null ? TargetTransform.localPosition : TargetTransform.position;
        initialRotation = useLocalSpace && TargetTransform.parent != null ? TargetTransform.localRotation : TargetTransform.rotation;

        isAnimating = true;

        DOVirtual.DelayedCall(delayBeforeStart, () =>
        {
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
                case MoveType.CustomDirection:
                    MoveCustomDirection();
                    break;
            }
        });
    }

    [Button, EnableIf(nameof(CanStopAnimation))]
    public async void StopMove(float delay = 0f)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        if (isAnimating)
        {
            Debug.LogWarning("Cannot stop animation while it is running!");
            return;
        }

        DOTween.Kill(TargetTransform);

        Vector3 currentPosition = useLocalSpace && TargetTransform.parent != null
            ? TargetTransform.localPosition
            : TargetTransform.position;

        Vector3 targetPosition = originalPosition;
        Quaternion currentRotation = useLocalSpace && TargetTransform.parent != null
            ? TargetTransform.localRotation
            : TargetTransform.rotation;

        Quaternion targetRotation = originalRotation;

        float returnDuration = moveDuration;
        _moveTween = TargetTransform
            .DOLocalMove(targetPosition, returnDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {
                IsAnimationFinished = false;
                isAnimating = false;
            });

        TargetTransform
            .DOLocalRotateQuaternion(targetRotation, returnDuration)
            .SetEase(Ease.InOutSine);
    }

    private void ResetToOriginalPosition()
    {
        if (useLocalSpace && TargetTransform.parent != null)
        {
            TargetTransform.localPosition = originalPosition;
            TargetTransform.localRotation = originalRotation;
        }
        else
        {
            TargetTransform.position = originalPosition;
            TargetTransform.rotation = originalRotation;
        }
    }

    private bool IsPlaying() => Application.isPlaying;

    private bool CanStartAnimation() => Application.isPlaying && !isAnimating;

    private bool CanStopAnimation() => Application.isPlaying && !isAnimating;

    public void SetMoveType(MoveType moveType) => MoveType = moveType;

    private void MoveForward()
    {
        Vector3 direction = useLocalSpace ? TargetTransform.forward * forwardDistance : Vector3.forward * forwardDistance;
        MoveInDirection(direction);
        
    }

    private void MoveCustomDirection()
    {
        Vector3 direction = useLocalSpace ? TargetTransform.TransformDirection(customDirection) : customDirection;
        MoveInDirection(direction);
    }

    private void MoveInDirection(Vector3 direction)
    {
        Vector3 startPosition = useLocalSpace && TargetTransform.parent != null ? TargetTransform.parent.TransformPoint(initialPosition) : initialPosition;
        Vector3 targetPosition = startPosition + direction;
        float progress = 0f;

        _moveTween = DOTween.To(() => progress, x => progress = x, 1f, moveDuration)
            .SetEase(Ease.InOutSine)
            .OnUpdate(() =>
            {
                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, progress);
                float curveValue = heightCurve.Evaluate(progress);
                if (curveValue != 0f)
                {
                    float heightOffset = curveValue * heightScale;
                    newPosition.y += heightOffset;
                }
                TargetTransform.position = newPosition;
            })
            .OnComplete(() =>
            {
                IsAnimationFinished = true;
                isAnimating = false;
                OnAnimationEnded?.Invoke(_targerColldier);
                AnimationEnded?.Invoke();
            });
    }

    private void MoveCircle()
    {
        float angle = 0f;
        Vector3 center = useLocalSpace && TargetTransform.parent != null
            ? TargetTransform.parent.TransformPoint(initialPosition)
            : initialPosition;

        _moveTween = DOTween.To(() => angle, x => angle = x, 360f, circleSpeed)
            .SetLoops(-1, LoopType.Restart)
            .OnUpdate(() =>
            {
                float progress = angle / 360f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 newPosition = center + new Vector3(
                    Mathf.Cos(rad) * circleRadius,
                    0,
                    Mathf.Sin(rad) * circleRadius
                );

                float curveValue = heightCurve.Evaluate(progress);
                if (curveValue != 0f)
                {
                    float heightOffset = curveValue * heightScale;
                    newPosition.y += heightOffset;
                }
                TargetTransform.position = newPosition;
                TargetTransform.LookAt(center);
            })
            .OnStepComplete(() => 
            {
                IsAnimationFinished = true;
                OnAnimationEnded?.Invoke(_targerColldier);
            });
    }

    private void MoveOrbitTarget()
    {
        if (orbitTarget == null)
        {
            Debug.LogWarning("OrbitTarget: ���� �� ������!");
            return;
        }

        float angle = 0f;
        Vector3 orbitCenter = orbitTarget.position;

        _moveTween = DOTween.To(() => angle, x => angle = x, 360f, moveDuration)
            .SetLoops(orbitTimes, LoopType.Restart)
            .OnUpdate(() =>
            {
                float progress = angle / 360f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 newPosition = useLocalSpace && orbitTarget.parent != null
                    ? orbitTarget.parent.TransformPoint(orbitCenter + new Vector3(
                        Mathf.Cos(rad) * orbitRadius,
                        0,
                        Mathf.Sin(rad) * orbitRadius))
                    : orbitCenter + new Vector3(
                        Mathf.Cos(rad) * orbitRadius,
                        0,
                        Mathf.Sin(rad) * orbitRadius);

                float curveValue = heightCurve.Evaluate(progress);
                if (curveValue != 0f)
                {
                    float heightOffset = curveValue * heightScale;
                    newPosition.y += heightOffset;
                }
                TargetTransform.position = newPosition;
                TargetTransform.LookAt(orbitCenter);
            })
            .OnComplete(() =>
            {
                IsAnimationFinished = true;
                isAnimating = false; // ��������� ����� ��������
                OnAnimationEnded?.Invoke(_targerColldier);
            });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CombatSystem player) && !isAnimating && !IsAnimationFinished)
        {
            StartMove();
            _targerColldier = other;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out CombatSystem player) && !isAnimating && !IsAnimationFinished)
        {
            StartMove();
            _targerColldier = other;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out CombatSystem player))
        {
            _targerColldier = null;
        }
    }
}