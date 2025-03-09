using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CameraInput
{
    public Vector2 Look;
}

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] private float sensitivity = 0.1f;
    
    private Vector3 _eulerAngles;

    public bool mayUpdate = true;
    public void Initialize(Transform target)
    {
        transform.position = target.position;
        transform.rotation = target.rotation;

        transform.eulerAngles = _eulerAngles = target.eulerAngles;

        Inventory.OnInventoryStateChange += value => mayUpdate = !value;
        CombatSystem.OnDie += () => isDead = true;
    }

    private bool isDead;

    public void UpdateRotation(CameraInput input)
    {
        if (isDead)
            return;
        if (!mayUpdate)
        {
            transform.eulerAngles = _eulerAngles;
            return;
        }
        _eulerAngles += new Vector3(-input.Look.y, input.Look.x) * sensitivity;
        _eulerAngles.x = Mathf.Clamp(_eulerAngles.x, -90, 90);
        transform.eulerAngles = _eulerAngles;
    }

    public void UpdatePosition(Transform target)
    {
        transform.position = target.position;
    }
}
