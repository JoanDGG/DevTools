using System;

namespace PixelPlay.Foundation.Input
{
    /// <summary>Reads logical input actions without exposing a particular input backend.</summary>
    public interface IInputActionReader : IDisposable
    {
        /// <summary>Enables the source's action set.</summary>
        void Enable();

        /// <summary>Disables the source's action set.</summary>
        void Disable();

        /// <summary>Gets whether the action is currently pressed.</summary>
        bool IsPressed(InputActionId action);

        /// <summary>Gets whether the action crossed its press threshold this frame.</summary>
        bool WasPressedThisFrame(InputActionId action);

        /// <summary>Gets whether the action crossed its release threshold this frame.</summary>
        bool WasReleasedThisFrame(InputActionId action);

        /// <summary>Reads the action as a float value.</summary>
        float ReadFloat(InputActionId action);

        /// <summary>Reads the action as a two-dimensional value.</summary>
        InputVector2 ReadVector2(InputActionId action);

        /// <summary>Reads the action as a three-dimensional value.</summary>
        InputVector3 ReadVector3(InputActionId action);

        /// <summary>Reads the action as a rotation value.</summary>
        InputQuaternion ReadQuaternion(InputActionId action);
    }
}
