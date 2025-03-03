
using NaughtyAttributes;
using UnityEngine;

public class Door : MonoBehaviour
{
    [field: Tooltip("Стена или любой другой объект, который нужно включать/выключать когда включается/отключается дверь")]
    [field: SerializeField] public Transform[] LinkedObjects { get; private set; } = null;
    [field: SerializeField] public GameObject DoorObject { get; private set; } = null;
    [field: SerializeField] public Transform ConnectPoint { get; private set; } = null;
    [field: SerializeField, ReadOnly] public bool Connected { get; private set; } = false;

    public void ChangeDoorState(bool opened)
    {
        DoorObject.SetActive(opened);

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
    }
    //При соединении комнат сразу у обоих делать ConnectRoom
}
