using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PixelPlay.Foundation.Debugging
{
    /// <summary>A typed game value that can be changed by a development tuning panel.</summary>
    public abstract class RuntimeTunable
    {
        protected RuntimeTunable(string id, string displayName, string category, string description)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tunable ID must not be null or whitespace.", nameof(id));

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName;
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category;
            Description = description ?? string.Empty;
        }

        /// <summary>Gets the unique ID used by game code to retrieve this value.</summary>
        public string Id { get; }

        /// <summary>Gets the label shown in the debug panel.</summary>
        public string DisplayName { get; }

        /// <summary>Gets the panel category used to group values.</summary>
        public string Category { get; }

        /// <summary>Gets the optional explanation shown with this value.</summary>
        public string Description { get; }

        /// <summary>Raised whenever this value changes.</summary>
        public event Action<RuntimeTunable> ValueChanged;

        /// <summary>Restores the value captured as its default at registration.</summary>
        public abstract void ResetToDefault();

        protected void NotifyValueChanged() => ValueChanged?.Invoke(this);
    }

    /// <summary>A tunable floating-point value with inclusive bounds.</summary>
    public sealed class FloatTunable : RuntimeTunable
    {
        private readonly float _defaultValue;

        internal FloatTunable(string id, string displayName, string category, string description,
            float defaultValue, float minimum, float maximum)
            : base(id, displayName, category, description)
        {
            if (float.IsNaN(minimum) || float.IsInfinity(minimum) ||
                float.IsNaN(maximum) || float.IsInfinity(maximum) || minimum > maximum)
                throw new ArgumentException("Tunable bounds must be finite and minimum must not exceed maximum.");
            if (float.IsNaN(defaultValue) || float.IsInfinity(defaultValue) ||
                defaultValue < minimum || defaultValue > maximum)
                throw new ArgumentOutOfRangeException(nameof(defaultValue), "Default must be within the tunable bounds.");

            Minimum = minimum;
            Maximum = maximum;
            _defaultValue = defaultValue;
            Value = defaultValue;
        }

        public float Minimum { get; }
        public float Maximum { get; }
        public float Value { get; private set; }

        public void SetValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < Minimum || value > Maximum)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be finite and within the tunable bounds.");
            if (Value.Equals(value))
                return;

            Value = value;
            NotifyValueChanged();
        }

        public override void ResetToDefault() => SetValue(_defaultValue);
    }

    /// <summary>A tunable integer value with inclusive bounds.</summary>
    public sealed class IntegerTunable : RuntimeTunable
    {
        private readonly int _defaultValue;

        internal IntegerTunable(string id, string displayName, string category, string description,
            int defaultValue, int minimum, int maximum)
            : base(id, displayName, category, description)
        {
            if (minimum > maximum)
                throw new ArgumentException("Minimum must not exceed maximum.");
            if (defaultValue < minimum || defaultValue > maximum)
                throw new ArgumentOutOfRangeException(nameof(defaultValue), "Default must be within the tunable bounds.");

            Minimum = minimum;
            Maximum = maximum;
            _defaultValue = defaultValue;
            Value = defaultValue;
        }

        public int Minimum { get; }
        public int Maximum { get; }
        public int Value { get; private set; }

        public void SetValue(int value)
        {
            if (value < Minimum || value > Maximum)
                throw new ArgumentOutOfRangeException(nameof(value), "Value must be within the tunable bounds.");
            if (Value == value)
                return;

            Value = value;
            NotifyValueChanged();
        }

        public override void ResetToDefault() => SetValue(_defaultValue);
    }

    /// <summary>A tunable boolean value.</summary>
    public sealed class BooleanTunable : RuntimeTunable
    {
        private readonly bool _defaultValue;

        internal BooleanTunable(string id, string displayName, string category, string description, bool defaultValue)
            : base(id, displayName, category, description)
        {
            _defaultValue = defaultValue;
            Value = defaultValue;
        }

        public bool Value { get; private set; }

        public void SetValue(bool value)
        {
            if (Value == value)
                return;

            Value = value;
            NotifyValueChanged();
        }

        public override void ResetToDefault() => SetValue(_defaultValue);
    }

    /// <summary>Owns per-project tunable values exposed to development tools.</summary>
    public sealed class RuntimeTuningRegistry
    {
        private readonly Dictionary<string, RuntimeTunable> _byId =
            new Dictionary<string, RuntimeTunable>(StringComparer.Ordinal);
        private readonly List<RuntimeTunable> _settings = new List<RuntimeTunable>();
        private readonly ReadOnlyCollection<RuntimeTunable> _readOnlySettings;

        public RuntimeTuningRegistry()
        {
            _readOnlySettings = _settings.AsReadOnly();
        }

        /// <summary>Gets tunables in registration order.</summary>
        public IReadOnlyList<RuntimeTunable> Settings => _readOnlySettings;

        public FloatTunable AddFloat(string id, string displayName, float defaultValue, float minimum, float maximum,
            string category = "General", string description = null)
        {
            return Register(new FloatTunable(id, displayName, category, description, defaultValue, minimum, maximum));
        }

        public IntegerTunable AddInteger(string id, string displayName, int defaultValue, int minimum, int maximum,
            string category = "General", string description = null)
        {
            return Register(new IntegerTunable(id, displayName, category, description, defaultValue, minimum, maximum));
        }

        public BooleanTunable AddBoolean(string id, string displayName, bool defaultValue,
            string category = "General", string description = null)
        {
            return Register(new BooleanTunable(id, displayName, category, description, defaultValue));
        }

        public FloatTunable GetFloat(string id) => Get<FloatTunable>(id);
        public IntegerTunable GetInteger(string id) => Get<IntegerTunable>(id);
        public BooleanTunable GetBoolean(string id) => Get<BooleanTunable>(id);

        /// <summary>Checks whether an ID is already registered.</summary>
        public bool Contains(string id) => !string.IsNullOrWhiteSpace(id) && _byId.ContainsKey(id);

        /// <summary>Restores every registered value to its initial value.</summary>
        public void ResetAll()
        {
            for (int i = 0; i < _settings.Count; i++)
                _settings[i].ResetToDefault();
        }

        private T Register<T>(T tunable) where T : RuntimeTunable
        {
            if (_byId.ContainsKey(tunable.Id))
                throw new ArgumentException($"A tunable with ID '{tunable.Id}' is already registered.", nameof(tunable));

            _byId.Add(tunable.Id, tunable);
            _settings.Add(tunable);
            return tunable;
        }

        private T Get<T>(string id) where T : RuntimeTunable
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tunable ID must not be null or whitespace.", nameof(id));
            if (!_byId.TryGetValue(id, out RuntimeTunable tunable))
                throw new KeyNotFoundException($"No tunable with ID '{id}' is registered.");
            if (!(tunable is T typed))
                throw new InvalidOperationException($"Tunable '{id}' is {tunable.GetType().Name}, not {typeof(T).Name}.");

            return typed;
        }
    }
}
