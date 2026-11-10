using System;
using System.Collections.Generic;
using PixelPlay.Foundation.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelPlay.Foundation.Unity.InputSystem
{
    /// <summary>Reads logical actions from one Unity Input System action map.</summary>
    /// <remarks>This reader owns the map's enabled state. Dispose it when its owner is destroyed.</remarks>
    public sealed class UnityInputActionReader : IInputActionReader
    {
        private readonly InputActionMap _actionMap;
        private readonly Dictionary<InputActionId, InputAction> _actions =
            new Dictionary<InputActionId, InputAction>();

        private bool _disposed;

        /// <summary>Creates a reader for the named map in an InputActionAsset.</summary>
        public UnityInputActionReader(InputActionAsset asset, string actionMapName)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            if (string.IsNullOrWhiteSpace(actionMapName))
                throw new ArgumentException("Action map name must not be null or whitespace.", nameof(actionMapName));

            _actionMap = asset.FindActionMap(actionMapName, true);
        }

        /// <summary>Gets whether this reader's action map is enabled.</summary>
        public bool IsEnabled => !_disposed && _actionMap.enabled;

        /// <inheritdoc />
        public void Enable()
        {
            ThrowIfDisposed();
            _actionMap.Enable();
        }

        /// <inheritdoc />
        public void Disable()
        {
            ThrowIfDisposed();
            _actionMap.Disable();
        }

        /// <inheritdoc />
        public bool IsPressed(InputActionId action) => FindAction(action).IsPressed();

        /// <inheritdoc />
        public bool WasPressedThisFrame(InputActionId action) => FindAction(action).WasPressedThisFrame();

        /// <inheritdoc />
        public bool WasReleasedThisFrame(InputActionId action) => FindAction(action).WasReleasedThisFrame();

        /// <inheritdoc />
        public float ReadFloat(InputActionId action) => FindAction(action).ReadValue<float>();

        /// <inheritdoc />
        public InputVector2 ReadVector2(InputActionId action)
        {
            Vector2 value = FindAction(action).ReadValue<Vector2>();
            return new InputVector2(value.x, value.y);
        }

        /// <inheritdoc />
        public InputVector3 ReadVector3(InputActionId action)
        {
            Vector3 value = FindAction(action).ReadValue<Vector3>();
            return new InputVector3(value.x, value.y, value.z);
        }

        /// <inheritdoc />
        public InputQuaternion ReadQuaternion(InputActionId action)
        {
            Quaternion value = FindAction(action).ReadValue<Quaternion>();
            return new InputQuaternion(value.x, value.y, value.z, value.w);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _actionMap.Disable();
            _actions.Clear();
            _disposed = true;
        }

        private InputAction FindAction(InputActionId actionId)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(actionId.ActionName))
                throw new ArgumentException("Action identifier is not initialized.", nameof(actionId));

            if (_actions.TryGetValue(actionId, out InputAction action))
                return action;

            action = _actionMap.FindAction(actionId.ActionName, true);
            _actions.Add(actionId, action);
            return action;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UnityInputActionReader));
        }
    }
}
