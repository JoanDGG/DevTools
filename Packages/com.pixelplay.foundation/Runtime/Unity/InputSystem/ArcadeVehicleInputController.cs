using System;
using PixelPlay.Foundation.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelPlay.Foundation.Unity.InputSystem
{
    /// <summary>Applies simple arcade-style acceleration, steering, reverse, and braking to a 2D or 3D vehicle body.</summary>
    /// <remarks>
    /// Bind a Vector2 action to steering on X and throttle on Y. The optional brake action should be a Button.
    /// This is a lightweight arcade controller, not a tire, suspension, or wheel simulation.
    /// </remarks>
    public sealed class ArcadeVehicleInputController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField, Tooltip("Asset containing the drive and optional brake actions.")] private InputActionAsset inputActions;
        [SerializeField, Tooltip("Action map containing Drive and Brake.")] private string actionMapName = "Vehicle";
        [SerializeField, Tooltip("Vector2 action: X is steering, Y is throttle/reverse.")] private string driveActionName = "Drive";
        [SerializeField, Tooltip("Optional Button action used to brake; leave empty to disable braking.")] private string brakeActionName = "Brake";

        [Header("Vehicle Body")]
        [SerializeField, Tooltip("Assign a 3D Rigidbody or a Rigidbody2D. Leave empty to find one on this GameObject.")] private Rigidbody body3D;
        [SerializeField] private Rigidbody2D body2D;
        [SerializeField, Tooltip("Allow negative throttle to drive in reverse.")] private bool allowReverse = true;

        [Header("Tuning")]
        [SerializeField, Min(0f), Tooltip("Acceleration in world units per second squared.")] private float acceleration = 12f;
        [SerializeField, Min(0f), Tooltip("Maximum linear speed in world units per second.")] private float maxSpeed = 20f;
        [SerializeField, Min(0f), Tooltip("Maximum steering angle in degrees per second at maximum speed.")] private float steeringDegreesPerSecond = 120f;
        [SerializeField, Min(0f), Tooltip("Brake damping strength; higher values stop the vehicle faster.")] private float brakeStrength = 8f;

        private UnityInputActionReader _input;
        private InputActionId _driveAction;
        private InputActionId _brakeAction;
        private bool _hasBrakeAction;
        private Vector2 _driveInput;
        private bool _brakeHeld;

        /// <summary>Gets the current speed magnitude.</summary>
        public float CurrentSpeed => body3D != null ? body3D.linearVelocity.magnitude : body2D != null ? body2D.linearVelocity.magnitude : 0f;

        /// <summary>Sets the maximum linear speed.</summary>
        public void SetMaxSpeed(float value)
        {
            ValidateNonNegative(value, nameof(value));
            maxSpeed = value;
        }

        /// <summary>Sets acceleration.</summary>
        public void SetAcceleration(float value)
        {
            ValidateNonNegative(value, nameof(value));
            acceleration = value;
        }

        /// <summary>Sets the maximum steering angle in degrees per second.</summary>
        public void SetSteeringDegreesPerSecond(float value)
        {
            ValidateNonNegative(value, nameof(value));
            steeringDegreesPerSecond = value;
        }

        /// <summary>Sets brake damping strength.</summary>
        public void SetBrakeStrength(float value)
        {
            ValidateNonNegative(value, nameof(value));
            brakeStrength = value;
        }

        /// <summary>Enables or disables reverse gear.</summary>
        public void SetAllowReverse(bool value) => allowReverse = value;

        /// <summary>Assigns a 3D physics body and clears the 2D body reference.</summary>
        public void SetBody(Rigidbody value)
        {
            body3D = value;
            body2D = null;
        }

        /// <summary>Assigns a 2D physics body and clears the 3D body reference.</summary>
        public void SetBody2D(Rigidbody2D value)
        {
            body2D = value;
            body3D = null;
        }

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("Assign an InputActionAsset to ArcadeVehicleInputController.", this);
                enabled = false;
                return;
            }

            if (body3D == null && body2D == null)
                body3D = GetComponent<Rigidbody>();
            if (body3D == null && body2D == null)
                body2D = GetComponent<Rigidbody2D>();
            if (body3D == null && body2D == null)
            {
                Debug.LogError("Assign a Rigidbody or Rigidbody2D to ArcadeVehicleInputController.", this);
                enabled = false;
                return;
            }

            _input = new UnityInputActionReader(inputActions, actionMapName);
            _driveAction = new InputActionId(driveActionName);
            if (!string.IsNullOrWhiteSpace(brakeActionName))
            {
                _brakeAction = new InputActionId(brakeActionName);
                _hasBrakeAction = true;
            }
        }

        private void OnEnable() => _input?.Enable();

        private void Update()
        {
            if (_input == null)
                return;

            InputVector2 drive = _input.ReadVector2(_driveAction);
            _driveInput = new Vector2(drive.X, drive.Y);
            _brakeHeld = _hasBrakeAction && _input.IsPressed(_brakeAction);
        }

        private void FixedUpdate()
        {
            if (body3D != null)
                Drive3D();
            else if (body2D != null)
                Drive2D();
        }

        private void Drive3D()
        {
            float throttle = allowReverse ? _driveInput.y : Mathf.Max(0f, _driveInput.y);
            float steer = Mathf.Clamp(_driveInput.x, -1f, 1f);
            if (_brakeHeld)
            {
                body3D.linearVelocity = Vector3.Lerp(body3D.linearVelocity, Vector3.zero,
                    1f - Mathf.Exp(-brakeStrength * Time.fixedDeltaTime));
                return;
            }

            body3D.AddForce(body3D.transform.forward * (throttle * acceleration), ForceMode.Acceleration);
            Vector3 verticalVelocity = Vector3.Project(body3D.linearVelocity, Vector3.up);
            Vector3 planarVelocity = Vector3.ProjectOnPlane(body3D.linearVelocity, Vector3.up);
            body3D.linearVelocity = Vector3.ClampMagnitude(planarVelocity, maxSpeed) + verticalVelocity;

            float direction = Mathf.Abs(throttle) > 0.01f ? Mathf.Sign(throttle) : Mathf.Sign(Vector3.Dot(planarVelocity, body3D.transform.forward));
            float speedFactor = maxSpeed > 0f ? Mathf.Clamp01(planarVelocity.magnitude / maxSpeed) : 0f;
            float turn = steer * steeringDegreesPerSecond * speedFactor * direction * Time.fixedDeltaTime;
            body3D.MoveRotation(body3D.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        private void Drive2D()
        {
            float throttle = allowReverse ? _driveInput.y : Mathf.Max(0f, _driveInput.y);
            float steer = Mathf.Clamp(_driveInput.x, -1f, 1f);
            if (_brakeHeld)
            {
                body2D.linearVelocity = Vector2.Lerp(body2D.linearVelocity, Vector2.zero,
                    1f - Mathf.Exp(-brakeStrength * Time.fixedDeltaTime));
                return;
            }

            body2D.AddForce((Vector2)body2D.transform.up * (throttle * acceleration), ForceMode2D.Force);
            body2D.linearVelocity = Vector2.ClampMagnitude(body2D.linearVelocity, maxSpeed);

            float forwardSpeed = Vector2.Dot(body2D.linearVelocity, body2D.transform.up);
            float direction = Mathf.Abs(throttle) > 0.01f ? Mathf.Sign(throttle) : Mathf.Sign(forwardSpeed);
            float speedFactor = maxSpeed > 0f ? Mathf.Clamp01(body2D.linearVelocity.magnitude / maxSpeed) : 0f;
            float turn = steer * steeringDegreesPerSecond * speedFactor * direction * Time.fixedDeltaTime;
            body2D.MoveRotation(body2D.rotation - turn);
        }

        private static void ValidateNonNegative(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and non-negative.");
        }

        private void OnDisable()
        {
            _driveInput = Vector2.zero;
            _brakeHeld = false;
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
