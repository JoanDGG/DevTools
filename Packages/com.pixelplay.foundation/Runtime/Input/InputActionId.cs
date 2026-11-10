using System;

namespace PixelPlay.Foundation.Input
{
    /// <summary>Identifies a logical input action independently of its physical bindings.</summary>
    public readonly struct InputActionId : IEquatable<InputActionId>
    {
        /// <summary>Creates an action identifier from its configured action name.</summary>
        public InputActionId(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                throw new ArgumentException("Action name must not be null or whitespace.", nameof(actionName));

            ActionName = actionName;
        }

        /// <summary>Gets the configured action name.</summary>
        public string ActionName { get; }

        /// <inheritdoc />
        public bool Equals(InputActionId other) =>
            StringComparer.OrdinalIgnoreCase.Equals(ActionName, other.ActionName);

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is InputActionId other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(ActionName ?? string.Empty);

        /// <inheritdoc />
        public override string ToString() => ActionName ?? string.Empty;
    }
}
