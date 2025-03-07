using NaughtyAttributes;
using UnityEngine;

public class Door : MonoBehaviour
{
    [field: Tooltip("Стена или любой другой объект, который нужно включать/выключать когда включается/отключается дверь")]
    [field: SerializeField] public Transform[] LinkedObjects { get; private set; } = null;
    [field: SerializeField] public EventDoor DoorEvent { get; private set; } = null;
    [field: SerializeField] public Transform ConnectPoint { get; private set; } = null;

    [field: InfoBox("To use it, enable Priority Doors in the Level Generator.")]
    [field: Tooltip("Priority of door usage (works for the PriorityDoors algorithm)")]
    [field: SerializeField, MinValue(0), MaxValue(100)] public float Priority { get; private set; } = 1;
    [field: Tooltip("Guarantees the connection of specific doors")]
    [field: SerializeField] public bool IsImportantConnection = false;
    [field: Space(10)]
    [field: SerializeField, ReadOnly] public bool Connected { get; private set; } = false;

    public void ChangeDoorState(bool opened)
    {
        DoorEvent.gameObject.SetActive(opened);

        if (LinkedObjects.Length <= 0)
            return;

        foreach (Transform wall in LinkedObjects)
        {
            if (wall == null)
                continue;

            wall.gameObject.SetActive(!opened);
        }
    }

    public void ConnectDoor(Door targetDoor)
    {
        Connected = true;
        targetDoor.Connected = true;

        ChangeDoorState(true);
        targetDoor.ChangeDoorState(true);
    }
    public void DisconnectDoor()
    {
        Connected = false;
        ChangeDoorState(false);
    }
}
