using System;
using PixelPlay.Foundation.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelPlay.Foundation.Unity.InputSystem
{
    /// <summary>Movement models supported by <see cref="InputSystemMovementController"/>.</summary>
    public enum CharacterMovementMode
    {
        /// <summary>Free movement on the XY plane, commonly used by 2D top-down games.</summary>
        TwoDTopDown,
        /// <summary>XY-plane movement projected onto diagonal isometric axes.</summary>
        TwoDIsometric,
        /// <summary>Horizontal movement with optional jump on a 2D physics body.</summary>
        TwoDSidescroller,
        /// <summary>Free movement on the XZ plane, commonly used by 3D top-down games.</summary>
        ThreeDTopDown,
        /// <summary>XZ-plane movement relative to a camera's flattened forward direction.</summary>
        ThreeDCameraRelative,
        /// <summary>Horizontal movement with optional jump on a 3D physics body.</summary>
        ThreeDSidescroller
    }

    /// <summary>Moves a target using a Vector2 action from Unity's Input System.</summary>
    /// <remarks>
    /// Top-down modes use Rigidbody2D/Rigidbody.MovePosition when available and otherwise move a Transform.
    /// Sidescroller modes preserve the physics body's vertical velocity and can use an optional ground check.
    /// For top-down physics, configure the body's gravity to zero in the Inspector.
    /// </remarks>
    public sealed class InputSystemMovementController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField, Tooltip("Asset containing the movement and optional jump actions.")] private InputActionAsset inputActions;
        [SerializeField, Tooltip("Action map containing Move and Jump.")] private string actionMapName = "Player";
        [SerializeField, Tooltip("Vector2 action used for movement.")] private string moveActionName = "Move";
        [SerializeField, Tooltip("Optional Button action used to jump in sidescroller modes. Leave empty to disable jumping.")] private string jumpActionName = "Jump";

        [Header("Movement")]
        [SerializeField] private CharacterMovementMode movementMode = CharacterMovementMode.ThreeDTopDown;
        [SerializeField, Tooltip("Optional object to move; defaults to this GameObject.")] private Transform target;
        [SerializeField, Tooltip("Camera used by ThreeD Camera Relative; defaults to Camera.main.")] private Transform movementCamera;
        [SerializeField, Min(0f), Tooltip("Horizontal movement speed in world units per second.")] private float moveSpeed = 5f;
        [SerializeField, Min(0f), Tooltip("Jump impulse applied in sidescroller modes; zero disables jumping.")] private float jumpImpulse = 6f;
        [SerializeField, Tooltip("Optional position used to check if the character is grounded. If empty, Rigidbody2D contact layers are used for 2D.")] private Transform groundCheck;
        [SerializeField, Min(0.01f), Tooltip("Radius of the optional ground check sphere/circle.")] private float groundCheckRadius = 0.2f;
        [SerializeField, Tooltip("Layers considered ground by the ground check.")] private LayerMask groundLayers = ~0;

        private UnityInputActionReader _input;
        private InputActionId _moveAction;
        private InputActionId _jumpAction;
        private Rigidbody2D _body2D;
        private Rigidbody _body3D;
        private Vector2 _moveInput;
        private bool _jumpRequested;
        private bool _hasJumpAction;

        /// <summary>Gets the selected movement mode.</summary>
        public CharacterMovementMode MovementMode => movementMode;

        /// <summary>Gets the current movement speed.</summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>Sets the movement model at runtime.</summary>
        public void SetMovementMode(CharacterMovementMode value)
        {
            if (!Enum.IsDefined(typeof(CharacterMovementMode), value))
                throw new ArgumentOutOfRangeException(nameof(value));
            movementMode = value;
        }

        /// <summary>Sets the movement model using the enum declaration index, for a UI Dropdown.</summary>
        public void SetMovementModeByIndex(int index)
        {
            if (!Enum.IsDefined(typeof(CharacterMovementMode), index))
                throw new ArgumentOutOfRangeException(nameof(index));
            movementMode = (CharacterMovementMode)index;
        }

        /// <summary>Sets movement speed in world units per second.</summary>
        public void SetMoveSpeed(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Movement speed must be finite and non-negative.");
            moveSpeed = value;
        }

        /// <summary>Sets the jump impulse used by sidescroller modes.</summary>
        public void SetJumpImpulse(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Jump impulse must be finite and non-negative.");
            jumpImpulse = value;
        }

        /// <summary>Sets the object moved by this controller.</summary>
        public void SetTarget(Transform value)
        {
            target = value;
            CacheBodies();
        }

        /// <summary>Sets the reference camera for ThreeD Camera Relative mode.</summary>
        public void SetMovementCamera(Transform value) => movementCamera = value;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("Assign an InputActionAsset to InputSystemMovementController.", this);
                enabled = false;
                return;
            }

            _input = new UnityInputActionReader(inputActions, actionMapName);
            _moveAction = new InputActionId(moveActionName);
            if (!string.IsNullOrWhiteSpace(jumpActionName))
            {
                _jumpAction = new InputActionId(jumpActionName);
                _hasJumpAction = true;
            }
            if (target == null)
                target = transform;
            if (movementCamera == null && Camera.main != null)
                movementCamera = Camera.main.transform;
            CacheBodies();
        }

        private void OnEnable() => _input?.Enable();

        private void Update()
        {
            if (_input == null)
                return;

            InputVector2 input = _input.ReadVector2(_moveAction);
            _moveInput = new Vector2(input.X, input.Y);
            if (_hasJumpAction && _input.WasPressedThisFrame(_jumpAction))
                _jumpRequested = true;
        }

        private void FixedUpdate()
        {
            if (target == null)
                return;

            switch (movementMode)
            {
                case CharacterMovementMode.TwoDTopDown:
                    Move2D(ClampInput(_moveInput));
                    break;
                case CharacterMovementMode.TwoDIsometric:
                    Move2D(ToIsometricDirection(_moveInput));
                    break;
                case CharacterMovementMode.TwoDSidescroller:
                    MoveSidescroller2D(_moveInput.x);
                    break;
                case CharacterMovementMode.ThreeDTopDown:
                    Move3D(new Vector3(_moveInput.x, 0f, _moveInput.y));
                    break;
                case CharacterMovementMode.ThreeDCameraRelative:
                    Move3D(ToCameraRelativeDirection(_moveInput));
                    break;
                case CharacterMovementMode.ThreeDSidescroller:
                    MoveSidescroller3D(_moveInput.x);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _jumpRequested = false;
        }

        private void Move2D(Vector2 direction)
        {
            Vector2 displacement = direction * moveSpeed * Time.fixedDeltaTime;
            if (_body2D != null)
                _body2D.MovePosition(_body2D.position + displacement);
            else
                target.position += new Vector3(displacement.x, displacement.y, 0f);
        }

        private void Move3D(Vector3 direction)
        {
            direction = Vector3.ClampMagnitude(direction, 1f);
            Vector3 displacement = direction * moveSpeed * Time.fixedDeltaTime;
            if (_body3D != null)
                _body3D.MovePosition(_body3D.position + displacement);
            else
                target.position += displacement;
        }

        private void MoveSidescroller2D(float horizontal)
        {
            if (_body2D == null)
            {
                target.position += Vector3.right * (horizontal * moveSpeed * Time.fixedDeltaTime);
                _jumpRequested = false;
                return;
            }

            Vector2 velocity = _body2D.linearVelocity;
            velocity.x = horizontal * moveSpeed;
            if (_jumpRequested && jumpImpulse > 0f && IsGrounded2D())
                velocity.y = jumpImpulse;
            _body2D.linearVelocity = velocity;
            _jumpRequested = false;
        }

        private void MoveSidescroller3D(float horizontal)
        {
            if (_body3D == null)
            {
                target.position += Vector3.right * (horizontal * moveSpeed * Time.fixedDeltaTime);
                _jumpRequested = false;
                return;
            }

            Vector3 velocity = _body3D.linearVelocity;
            velocity.x = horizontal * moveSpeed;
            if (_jumpRequested && jumpImpulse > 0f && IsGrounded3D())
                velocity.y = jumpImpulse;
            _body3D.linearVelocity = velocity;
            _jumpRequested = false;
        }

        private bool IsGrounded2D()
        {
            if (groundCheck != null)
                return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayers) != null;
            return _body2D.IsTouchingLayers(groundLayers);
        }

        private bool IsGrounded3D()
        {
            return groundCheck != null && Physics.CheckSphere(groundCheck.position, groundCheckRadius,
                groundLayers, QueryTriggerInteraction.Ignore);
        }

        private Vector3 ToCameraRelativeDirection(Vector2 input)
        {
            Transform cameraTransform = movementCamera != null ? movementCamera : transform;
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            return Vector3.ClampMagnitude(right * input.x + forward * input.y, 1f);
        }

        private static Vector2 ToIsometricDirection(Vector2 input)
        {
            Vector2 direction = new Vector2(input.x - input.y, (input.x + input.y) * 0.5f);
            return Vector2.ClampMagnitude(direction, 1f);
        }

        private static Vector2 ClampInput(Vector2 input) => Vector2.ClampMagnitude(input, 1f);

        private void CacheBodies()
        {
            if (target == null)
                return;
            _body2D = target.GetComponent<Rigidbody2D>();
            _body3D = target.GetComponent<Rigidbody>();
        }

        private void OnDisable()
        {
            _jumpRequested = false;
            if (_input != null && _input.IsEnabled)
                _input.Disable();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
            _input = null;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
