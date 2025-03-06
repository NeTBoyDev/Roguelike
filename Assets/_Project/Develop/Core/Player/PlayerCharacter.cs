    using System.Collections;
    using System.Collections.Generic;
    using KinematicCharacterController;
    using TMPro;
    using UnityEngine;

    public enum CrouchInput
    {
        None, Toggle, Press
    }

    public enum Stance
    {
        Stand,Crouch,Slide
    }

    public struct CharacterState
    {
        public bool Grounded;
        public Stance Stance;
        public Vector3 Velocity;
        public Vector3 Acceleration;
    }

    public struct CharacterInput
    {
        public Quaternion Rotation;
        public Vector2 Move;
        public bool Jump;
        public bool JumpSustain;
        public CrouchInput Crouch;
        
    }

    public class PlayerCharacter : MonoBehaviour, ICharacterController
    {
        public Vector3 velocity { get; private set; }
        
        [SerializeField] private KinematicCharacterMotor _motor;
        [SerializeField] private Transform cameraTargert;
        [SerializeField] private Transform root;
        [Space] 
        
        [SerializeField] private float walkSpeed = 20f;
        [SerializeField] private float crouchSpeed = 7f;
        [SerializeField] private float crouchResponse= 25f;
        [SerializeField] private float walkResponse = 20f;
        
        [Space]
        [SerializeField] private float jumpSpeed = 20f;
        [SerializeField] private float coyoteTime = 0.2f;
        [Range(0,1)]
        [SerializeField] private float jumpSustainGravity = 0.4f;
        [SerializeField] private float gravity = -90f;
        [Space] 
        [SerializeField] private float slideStartSpeed = 25f;
        [SerializeField] private float slideEndSpeed = 15f;
        [SerializeField] private float slideFriction = 0.8f;
        [SerializeField] private float slideSteerAcceleration = 5f;
        [SerializeField] private float slideGravity = -90f;
        [Space] 
        [SerializeField] private float standHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchHeightResponse = 15f;
        [Space] 
        [SerializeField] private float airSpeed = 15f;
        [SerializeField] private float airAcceleration = 70f;
        [Space]
        [Range(0,1)]
        [SerializeField] private float standCameraTargetHeight = 0.9f;
        [Range(0,1)]
        [SerializeField] private float crouchCameraTargetHeight = 0.7f;

        private CharacterState _state;
        private CharacterState _lastState;
        private CharacterState _tempState;

        private Quaternion _requestedRotation;
        private Vector3 _requestedMovement;
        private bool _requestedJump;
        private bool _requestedSustainedJump;
        private bool _requestedCrouch;
        private bool _requestedCrouchInAir;
        private bool _requestedThrow;
        private Vector3 _throwDirection;
        private float _throwForce;

        private float _timeSinceUngrounded;
        private float _timeSinceJumpRequest;
        private bool _ungroundedDueToJump;

        private Collider[] _uncrouchOverlapResults;
        
        // Новые поля для механики пружины
        [SerializeField] private Transform ropeConnectedObject; // Объект, к которому будет прикреплена веревка
        [SerializeField] private float ropeLength = 5f; // Длина веревки
        [SerializeField] private float maxRopeForce = 50f; // Максимальная сила, которую может приложить веревка
        [SerializeField] private float RopeForce = 50f; // Максимальная сила, которую может приложить веревка
        [SerializeField] private float ropeSpeed = 5f; // Максимальная скорость притяжения
        [SerializeField] private float ropeDamping = 0.5f; // Дампинг веревки для уменьшения колебаний
        [SerializeField] private float ropeLengthMultiplier = 3f; // Дампинг веревки для уменьшения колебаний
        [SerializeField] private float ropeDistanceMultiplier = 3f; // Дампинг веревки для уменьшения колебаний


        private Vector3 _ropeForce; // Сила веревк

        public void Initialize()
        {
            _state.Stance = Stance.Stand;
            _lastState = _state;
            _uncrouchOverlapResults = new Collider[8];
            
            _motor.CharacterController = this;
            
            
        }

        public void SetSpeed(float value)
        {
            walkSpeed = value;
            airSpeed = value;
        }

        public void UpdateInput(CharacterInput input)
        {
            _requestedRotation = input.Rotation;
            _requestedMovement = new Vector3(input.Move.x, 0, input.Move.y);
            _requestedMovement = Vector3.ClampMagnitude(_requestedMovement, 1f);
            _requestedMovement = input.Rotation * _requestedMovement;

            var wasRequestingJump = _requestedJump;
            _requestedJump = _requestedJump || input.Jump;

            if (_requestedJump && !wasRequestingJump)
            {
                _timeSinceJumpRequest = 0f;
            }
            _requestedSustainedJump = input.JumpSustain;

            var wasRequestingCrouch = _requestedCrouch;
            _requestedCrouch = input.Crouch switch
            {
                CrouchInput.Press => true,
                CrouchInput.None => false,
                _ => _requestedCrouch
            };

            if (_requestedCrouch && !wasRequestingCrouch)
            {
                _requestedCrouchInAir = !_state.Grounded;
            }
            else if (!_requestedCrouch && wasRequestingCrouch)
            {
                _requestedCrouchInAir = false;
            }
        }

        public void UpdateBody(float deltaTime)
        {
            var currentHeight = _motor.Capsule.height;
            var normalizedHeight = currentHeight / standHeight;
            var cameraTargetHeight = currentHeight * (_state.Stance is Stance.Stand
                ? standCameraTargetHeight
                : crouchCameraTargetHeight);
            var rootTargetScale = new Vector3(1f, normalizedHeight, 1f);

            cameraTargert.localPosition = Vector3.Lerp(cameraTargert.localPosition, new Vector3(0f, cameraTargetHeight, 0f),
                1f - Mathf.Exp(-crouchHeightResponse * deltaTime));
            root.localScale = Vector3.Lerp(root.localScale,rootTargetScale,1f - Mathf.Exp(-crouchHeightResponse * deltaTime));
        }
        
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {

            var forward = Vector3.ProjectOnPlane(_requestedRotation * Vector3.forward,
                _motor.CharacterUp);
            
            if(forward != Vector3.zero)
                currentRotation = Quaternion.LookRotation(forward,_motor.CharacterUp);
            
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            _state.Acceleration = Vector3.zero;
            if (_motor.GroundingStatus.IsStableOnGround)
            {
                _timeSinceUngrounded = 0f;
                _ungroundedDueToJump = false;
                var groundMovemend =
                    _motor.GetDirectionTangentToSurface(direction: _requestedMovement,
                        surfaceNormal: _motor.GroundingStatus.GroundNormal) * _requestedMovement.magnitude;
                {
                    var moving = groundMovemend.sqrMagnitude > 0f;
                    var crouching = _state.Stance is Stance.Crouch;
                    var wasStanding = _lastState.Stance is Stance.Stand;
                    var wasInAir = !_lastState.Grounded;
                    if (moving && crouching && (wasStanding || wasInAir))
                    {
                        _state.Stance = Stance.Slide;

                        if (wasInAir)
                        {
                            currentVelocity =
                                Vector3.ProjectOnPlane(_lastState.Velocity, _motor.GroundingStatus.GroundNormal);
                        }

                        var effectiveSlideStartSpeed = slideStartSpeed;

                        if (!_lastState.Grounded && !_requestedCrouchInAir)
                        {
                            effectiveSlideStartSpeed = 0f;
                            _requestedCrouchInAir = false;
                        }
                        var slideSpeed = Mathf.Max(slideStartSpeed,currentVelocity.magnitude);
                        currentVelocity = _motor.GetDirectionTangentToSurface(currentVelocity,
                            _motor.GroundingStatus.GroundNormal) * slideSpeed;
                    }
                }
                if(_state.Stance is Stance.Stand or Stance.Crouch)
                {
                    var speed = _state.Stance is Stance.Stand ? walkSpeed : crouchSpeed;

                    var response = _state.Stance is Stance.Stand
                        ? walkResponse
                        : crouchResponse;

                    var targetVelocity = groundMovemend * speed;
                    var moveVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-response * deltaTime));
                    _state.Acceleration = moveVelocity - currentVelocity;
                    currentVelocity = moveVelocity;
                }
                else
                {
                    currentVelocity -= currentVelocity * (slideFriction * deltaTime);
                    {
                        var force = Vector3.ProjectOnPlane(-_motor.CharacterUp, _motor.GroundingStatus.GroundNormal) *
                                    slideGravity;
                        currentVelocity -= force * deltaTime;
                    }
                    {
                        var currentSpeed = currentVelocity.magnitude;
                        var targetVelocity = groundMovemend * currentSpeed;
                        var steerVelocity = currentVelocity;
                        var steerForce = (targetVelocity - steerVelocity) * slideSteerAcceleration * deltaTime;
                        steerVelocity += steerForce;
                        steerVelocity = Vector3.ClampMagnitude(steerVelocity, currentSpeed);

                        _state.Acceleration = (steerVelocity - currentVelocity) / deltaTime;
                        currentVelocity = steerVelocity;
                    }

                    if (currentVelocity.magnitude < slideEndSpeed)
                        _state.Stance = Stance.Crouch;
                }
            }
            else
            {
                _timeSinceUngrounded += deltaTime;
                if (_requestedMovement.sqrMagnitude > 0f)
                {
                    var planarMovement = Vector3.ProjectOnPlane(_requestedMovement,
                        _motor.CharacterUp) * _requestedMovement.magnitude;

                    var currentPlanarVelocity = Vector3.ProjectOnPlane(currentVelocity, _motor.CharacterUp);

                    var movementForce = planarMovement * airAcceleration * deltaTime;

                    if (currentPlanarVelocity.magnitude < airSpeed)
                    {
                        var targetPlanarVelocity = currentPlanarVelocity + movementForce;

                        targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, airSpeed);

                        movementForce = targetPlanarVelocity - currentPlanarVelocity;
                    }
                    else if (Vector3.Dot(currentPlanarVelocity, movementForce) > 0f)
                    {
                        var constrainedMovementForce =
                            Vector3.ProjectOnPlane(movementForce, currentPlanarVelocity.normalized);

                        movementForce = constrainedMovementForce;
                    }

                    if (_motor.GroundingStatus.FoundAnyGround)
                    {
                        if (Vector3.Dot(movementForce, currentVelocity + movementForce) > 0f)
                        {
                            var obstructionNormal = Vector3.Cross(
                                _motor.CharacterUp,
                                Vector3.Cross(
                                    _motor.CharacterUp, _motor.GroundingStatus.GroundNormal)).normalized;

                            movementForce = Vector3.ProjectOnPlane(movementForce, obstructionNormal);
                        }
                    }

                    currentVelocity += movementForce;
                }
                
                var effectiveGravity = gravity;
                var verticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
                if (_requestedSustainedJump && verticalSpeed > 0f)
                    effectiveGravity *= jumpSustainGravity;
                currentVelocity += _motor.CharacterUp * effectiveGravity * deltaTime;
            }

            if (_requestedJump)
            {
                var grounded = _motor.GroundingStatus.IsStableOnGround;
                var canCayoteJump = _timeSinceUngrounded < coyoteTime && !_ungroundedDueToJump;
                
                if(grounded || canCayoteJump){
                    _requestedJump = false;
                    _requestedCrouch = false;
                    _requestedCrouchInAir = false;
                
                    _motor.ForceUnground(0f);
                    _ungroundedDueToJump = true;

                    var currentVerticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
                    var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);

                    currentVelocity += _motor.CharacterUp * (targetVerticalSpeed - currentVerticalSpeed);
                }
                else
                {
                    _timeSinceJumpRequest += deltaTime;
                    var canJumpLater = _timeSinceJumpRequest < coyoteTime;
                    
                    _requestedJump = canJumpLater;
                }
                
            }
            
            if (_requestedThrow)
            {
                _requestedThrow = false;
                _requestedCrouch = false;
                _requestedCrouchInAir = false;
                
                _motor.ForceUnground(0f);

                // Нормализация направления броска (если еще не нормализовано)
                var normalizedThrowDirection = _throwDirection.normalized;

                var currentVerticalSpeed = Vector3.Dot(currentVelocity, _motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed, jumpSpeed);
                
                print($"Dif {(targetVerticalSpeed - currentVerticalSpeed)}");
                print($"Vel = {normalizedThrowDirection * _throwForce * (targetVerticalSpeed - currentVerticalSpeed)}");

                currentVelocity.y = 0;
                // Применяем силу броска
                currentVelocity += normalizedThrowDirection * _throwForce ;
            }
            
            if (ropeConnectedObject != null)
            {
                Vector3 toRopeObject = ropeConnectedObject.position - _motor.TransientPosition;
                float distanceToRopeObject = toRopeObject.magnitude;

                if (distanceToRopeObject > ropeLength)
                {
                    float excessDistance = distanceToRopeObject - ropeLength;
                    float ropeForceMagnitude = Mathf.Clamp(excessDistance * (maxRopeForce / ropeLength), 0f, maxRopeForce);
                    Vector3 ropeForceDirection = toRopeObject.normalized;

                    _ropeForce = ropeForceDirection * ropeForceMagnitude;

                    Vector3 dampingForce = currentVelocity * ropeDamping;

                    Vector3 effectiveRopeForce = _ropeForce - dampingForce;
                    Vector3 velocityToRopeObject = effectiveRopeForce * deltaTime;

                    if (velocityToRopeObject.magnitude > ropeSpeed)
                    {
                        velocityToRopeObject = velocityToRopeObject.normalized * ropeSpeed;
                    }

                    currentVelocity += velocityToRopeObject;
                }
            }

            velocity = currentVelocity;
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            _tempState = _state;
            if (_requestedCrouch && _state.Stance is Stance.Stand)
            {
                _state.Stance = Stance.Crouch;
                _motor.SetCapsuleDimensions(radius:_motor.Capsule.radius,height: crouchHeight,
                    yOffset: crouchHeight * 0.5f);
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            if (!_motor.GroundingStatus.IsStableOnGround && _state.Stance is Stance.Slide)
            {
                _state.Stance = Stance.Crouch;
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            if (!_requestedCrouch && _state.Stance is not Stance.Stand)
            {
                
                _motor.SetCapsuleDimensions(radius:_motor.Capsule.radius,height: standHeight,
                    yOffset: standHeight * 0.5f);

                if (_motor.CharacterOverlap(_motor.TransientPosition, _motor.TransientRotation, _uncrouchOverlapResults,
                        _motor.CollidableLayers, QueryTriggerInteraction.Ignore) > 0)
                {
                    _requestedCrouch = true;
                    _motor.SetCapsuleDimensions(radius:_motor.Capsule.radius,height: crouchHeight,
                        yOffset: crouchHeight * 0.5f);
                }
                else
                {
                    _state.Stance = Stance.Stand;
                }

                
            }

            _state.Grounded = _motor.GroundingStatus.IsStableOnGround;
            _state.Velocity = _motor.Velocity;
            _lastState = _tempState;
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        public Transform GetCameraTarget() => cameraTargert;

        public void SetPosition(Vector3 position, bool killVelocity = true)
        {
            _motor.SetPosition(position);
            if (killVelocity)
                _motor.BaseVelocity = Vector3.zero;
        }

        public void Throw(Vector3 direction, float force)
        {
            _throwDirection = direction.normalized; //УБРАЛ НОРМАЛИЗАЦИЮ ВЕКТОРА
            _requestedThrow = true;
            _throwForce = force;
        }

        public CharacterState GetState() => _state;
        public CharacterState GetLastState => _lastState;

        public void SetSpringTarget(Transform transform)
        {
            ropeConnectedObject = transform;
            if (transform == null)
                return;
            
            var distance = Vector3.Distance(transform.position, this.transform.position);
            ropeLength = distance / ropeLengthMultiplier;
            maxRopeForce = RopeForce / ropeDistanceMultiplier;
        }
    }
