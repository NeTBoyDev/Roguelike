using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public class EventDoor : MonoBehaviour
{
    [SerializeField] private Transform door;
    [SerializeField] private Collider[] triggers;

    [Header("Settings")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float duration = 1f;
    [SerializeField] private bool debugMode = true;

    [field: SerializeField, ReadOnly] public bool IsOpened { get; private set; } = false;
    [SerializeField, ReadOnly] private float _lastRotationDirection = 0f;
    [SerializeField, ReadOnly] private bool _isCurrentlyOpen = false;


    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out CombatSystem player) && !_isCurrentlyOpen)
        {
            OpenDoor(player.transform.position);
        }
    }

    private void OpenDoor(Vector3 playerPosition)
    {
        if (_isCurrentlyOpen) return;

        Vector3 doorToPlayer = playerPosition - door.position;
        doorToPlayer.y = 0;

        Vector3 doorForward = door.forward;
        doorForward.y = 0;
        doorForward.Normalize();

        float sideDot = Vector3.Dot(doorToPlayer.normalized, door.right);
        _lastRotationDirection = sideDot >= 0 ? -1f : 1f;

        if (debugMode)
        {
            Debug.Log($"Door opening direction: {_lastRotationDirection}, sideDot: {sideDot}");
        }

        float targetAngle = _lastRotationDirection * openAngle;

        door.DORotate(new Vector3(0, -targetAngle, 0), duration)
            .SetRelative(true)
            .OnStart(() =>
            {
                _isCurrentlyOpen = true;
                IsOpened = true;
                ChangeTriggers(false);
            });
    }
    public void CloseDoor()
    {
        if (!_isCurrentlyOpen) return;

        door.DORotate(Vector3.zero, duration)
            .OnStart(() => ChangeTriggers(true))
            .OnComplete(() => _isCurrentlyOpen = false);
    }

    public void ReopenDoor()
    {
        if (_isCurrentlyOpen || _lastRotationDirection == 0f) return;

        float targetAngle = _lastRotationDirection * openAngle;

        door.DORotate(new Vector3(0, targetAngle, 0), duration)
            .SetRelative(true)
            .OnStart(() =>
            {
                _isCurrentlyOpen = true;
                ChangeTriggers(false);
            });
    }

    private void ChangeTriggers(bool value)
    {
        foreach (Collider trigger in triggers)
        {
            if (trigger != null)
            {
                trigger.enabled = value;
            }
        }
    }

    public void ResetDoor()
    {
        door.DOKill();
        door.localRotation = Quaternion.identity;

        _isCurrentlyOpen = false;
        _lastRotationDirection = 0f;

        IsOpened = false;
        ChangeTriggers(true);
    }

    private void OnDrawGizmosSelected()
    {
        if (door != null)
        {
            // Отображаем направления
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(door.position, door.forward * 1f); // Нормаль двери

            Gizmos.color = Color.red;
            Gizmos.DrawRay(door.position, door.right * 1f); // Вправо

            // Отображаем пивот
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(door.position, 0.1f);

            // Отображаем центр меша
            MeshFilter meshFilter = door.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Vector3 localCenter = meshFilter.sharedMesh.bounds.center;
                Vector3 worldCenter = door.TransformPoint(localCenter);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(worldCenter, 0.1f);
            }
        }
    }
}