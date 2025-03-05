using _Project.Develop.Core.Player;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private PlayerCharacter _playerCharacter;

    [SerializeField] private PlayerCamera _playerCamera;
    [Space] 
    [SerializeField] private CameraSpring _cameraSpring;
    [SerializeField] private CameraLean _cameraLean;

    [SerializeField] private Animator animator;
    [SerializeField] private AudioClip[] _stepClips;
    private SoundManager _soundManager = new(0.65f);

    private float lastStepTime;

    private float stepCooldown = .65f;
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        _playerCharacter.Initialize();
        _playerCamera.Initialize(_playerCharacter.GetCameraTarget());
        
        _cameraSpring.Initialize();
        _cameraLean.Initialize();

        lastStepTime = Time.time;

    }


    // Update is called once per frame
    private Transform obj;
    void Update()
    {
        var deltaTime = Time.deltaTime;

        var cameraInput = new CameraInput() { Look = new Vector2(Input.GetAxis("Mouse X"),Input.GetAxis("Mouse Y")) };
        _playerCamera.UpdateRotation(cameraInput);

        var characterInput = new CharacterInput()
        {
            Rotation = _playerCamera.transform.rotation,
            Move     = new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical")),
            Jump     = Input.GetKeyDown(KeyCode.Space),
            JumpSustain = /*Input.GetKey(KeyCode.Space)*/ false,
            Crouch = /*input.Crouch.WasPressedThisFrame()
            ? CrouchInput.Toggle :*/
                /*Input.GetKey(KeyCode.LeftControl) ? CrouchInput.Press:*/ CrouchInput.None
            
        };
        print(characterInput.Move.magnitude);
        if (characterInput.Move.magnitude > 0)
        {
            if (lastStepTime + stepCooldown < Time.time)
            {
                lastStepTime = Time.time;
                _soundManager.ProduceSound(_playerCharacter.transform.position,_stepClips[Random.Range(0,_stepClips.Length)]);
            }
        }
        
        
        _playerCharacter.UpdateInput(characterInput);
        _playerCharacter.UpdateBody(deltaTime);
        animator.SetFloat("MoveX",Input.GetAxis("Vertical"));
        

#if UNITY_EDITOR
        /*if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            _playerCharacter.Throw(-_playerCamera.transform.forward,2);
        }*/

        
#endif
        
    }

    private void LateUpdate()
    {
        var cameraTarget = _playerCharacter.GetCameraTarget();
        var state = _playerCharacter.GetState();
        
        _playerCamera.UpdatePosition(cameraTarget);
        _cameraSpring.UpdateSpring(Time.deltaTime,cameraTarget.up);
        _cameraLean.UpdateLean(Time.deltaTime,state.Acceleration,cameraTarget.up);
    }

    private void OnDestroy()
    {
    }

    public void Teleport(Vector3 pos)
    {
        _playerCharacter.SetPosition(pos);
    }
}
