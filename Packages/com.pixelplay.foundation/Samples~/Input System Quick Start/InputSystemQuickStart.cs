using PixelPlay.Foundation.Input;
using PixelPlay.Foundation.Unity.InputSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Demonstrates device-independent movement and confirmation actions.</summary>
    public sealed class InputSystemQuickStart : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string actionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string confirmActionName = "Submit";
        [SerializeField] private Transform movementTarget;
        [SerializeField, Min(0f)] private float movementSpeed = 4f;

        private UnityInputActionReader _input;
        private InputActionId _moveAction;
        private InputActionId _confirmAction;

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
