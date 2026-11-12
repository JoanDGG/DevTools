using PixelPlay.Foundation.Input;
using PixelPlay.Foundation.Unity.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Demonstrates device-independent movement and confirmation actions.</summary>
    public sealed class InputSystemQuickStart : MonoBehaviour
    {
        [SerializeField, Tooltip("Input Action Asset containing the actions used by this sample.")] private InputActionAsset inputActions;
        [SerializeField, Tooltip("Action map that contains the movement and confirm actions.")] private string actionMapName = "Player";
        [SerializeField, Tooltip("Vector2 action read to move the target.")] private string moveActionName = "Move";
        [SerializeField, Tooltip("Button action that logs a confirmation when pressed.")] private string confirmActionName = "Submit";
        [SerializeField, Tooltip("Transform moved by the sample; leave empty to log movement only.")] private Transform movementTarget;
        [SerializeField, Min(0f), Tooltip("Movement distance per second in world units.")] private float movementSpeed = 4f;

        private UnityInputActionReader _input;
        private InputActionId _moveAction;
        private InputActionId _confirmAction;

        /// <summary>Gets the current world-space movement speed.</summary>
        public float MovementSpeed => movementSpeed;

        /// <summary>Changes movement speed at runtime, for example from a settings slider.</summary>
        public void SetMovementSpeed(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new System.ArgumentOutOfRangeException(nameof(value), "Movement speed must be finite and non-negative.");
            movementSpeed = value;
        }

        /// <summary>Changes the object moved by this sample.</summary>
        public void SetMovementTarget(Transform value) => movementTarget = value;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("Assign an InputActionAsset to InputSystemQuickStart.", this);
                enabled = false;
                return;
            }

            _input = new UnityInputActionReader(inputActions, actionMapName);
            _moveAction = new InputActionId(moveActionName);
            _confirmAction = new InputActionId(confirmActionName);
        }

        private void OnEnable() => _input?.Enable();

        private void Update()
        {
            if (_input == null)
                return;
            InputVector2 movement = _input.ReadVector2(_moveAction);
            Transform target = movementTarget != null ? movementTarget : transform;
            target.position += new Vector3(movement.X, 0f, movement.Y) * movementSpeed * Time.deltaTime;
            if (_input.WasPressedThisFrame(_confirmAction))
                Debug.Log("Confirm action performed.", this);
        }

        private void OnDisable()
        {
            if (_input != null && _input.IsEnabled)
                _input.Disable();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
            _input = null;
        }
    }
}
