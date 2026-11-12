using System;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PixelPlay.Foundation.Unity.UI
{
    /// <summary>
    /// Converts text from a UI input field to the parameter type of a public setter method
    /// and invokes that method on a configured component.
    /// </summary>
    /// <remarks>
    /// Connect a TMP_InputField or legacy InputField OnValueChanged (string) event to
    /// <see cref="SetFromString"/>, or assign a TMP_InputField and call <see cref="ApplyInputField"/>
    /// from a button. Supported setter parameter types are string, bool,
    /// enums, and numeric primitive types. Numeric parsing uses invariant culture by default.
    /// </remarks>
    public sealed class StringInputSetter : MonoBehaviour
    {
        [SerializeField, Tooltip("Component that owns the public setter method.")]
        private MonoBehaviour target;
        [SerializeField, Tooltip("TMP input field read by ApplyInputField; assign this to call the bridge from a Button.")]
        private TMP_InputField inputField;
        [SerializeField, Tooltip("Name of a public instance method with exactly one supported parameter, for example SetTimeoutSeconds.")]
        private string setterMethodName;
        [SerializeField, Tooltip("Use the device's current culture when parsing numeric values instead of invariant culture.")]
        private bool useCurrentCulture;
        [SerializeField, Tooltip("Invoked when the text was converted and the setter was called; passes the original input.")]
        private UnityEvent<string> onValueApplied = new UnityEvent<string>();
        [SerializeField, Tooltip("Invoked when target lookup, conversion, or setter invocation fails.")]
        private UnityEvent<string> onApplyFailed = new UnityEvent<string>();

        private MethodInfo _cachedSetter;
        private Type _cachedParameterType;

        /// <summary>Gets the event raised after a value has been successfully applied.</summary>
        public UnityEvent<string> OnValueApplied => onValueApplied;

        /// <summary>Gets the event raised when conversion or setter invocation fails.</summary>
        public UnityEvent<string> OnApplyFailed => onApplyFailed;

        /// <summary>Assigns the component that owns the configured setter.</summary>
        public void SetTarget(MonoBehaviour value)
        {
            target = value;
            InvalidateSetterCache();
        }

        /// <summary>Assigns the TMP input field read by <see cref="ApplyInputField"/>.</summary>
        public void SetInputField(TMP_InputField value) => inputField = value;

        /// <summary>Sets the public one-argument method name to invoke.</summary>
        public void SetSetterMethodName(string value)
        {
            setterMethodName = value?.Trim() ?? string.Empty;
            InvalidateSetterCache();
        }

        /// <summary>Chooses whether numbers use invariant or device-current culture.</summary>
        public void SetUseCurrentCulture(bool value) => useCurrentCulture = value;

        /// <summary>Reads the configured TMP input field and applies its current text.</summary>
        /// <remarks>Connect this parameterless method to a Button's On Click event.</remarks>
        public void ApplyInputField()
        {
            if (inputField == null)
            {
                ReportFailure("Assign a TMP_InputField before applying its value.");
                return;
            }

            SetFromInputField(inputField);
        }

        /// <summary>Reads the supplied TMP input field and applies its current text.</summary>
        /// <param name="inputField">Input field whose text should be converted and applied.</param>
        public void SetFromInputField(TMP_InputField inputField)
        {
            if (inputField == null)
            {
                ReportFailure("The TMP_InputField argument cannot be null.");
                return;
            }
            SetFromString(inputField.text);
        }

        /// <summary>Converts text and invokes the configured setter; intended for a UI OnValueChanged event.</summary>
        public void SetFromString(string value)
        {
            try
            {
                ResolveSetter();
                object convertedValue = ConvertValue(value ?? string.Empty, _cachedParameterType);
                _cachedSetter.Invoke(target, new[] { convertedValue });
                onValueApplied?.Invoke(value ?? string.Empty);
            }
            catch (Exception exception)
            {
                string message = GetUsefulMessage(exception);
                Debug.LogWarning($"Could not apply input value: {message}", this);
                onApplyFailed?.Invoke(message);
            }
        }

        private void ResolveSetter()
        {
            if (_cachedSetter != null)
                return;
            if (target == null)
                throw new InvalidOperationException("Assign a target component before applying input.");
            if (string.IsNullOrWhiteSpace(setterMethodName))
                throw new InvalidOperationException("Configure the public setter method name before applying input.");

            MethodInfo[] methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            MethodInfo match = null;
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo candidate = methods[i];
                if (candidate.Name != setterMethodName)
                    continue;

                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != 1 || !IsSupportedType(parameters[0].ParameterType))
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"More than one supported overload named '{setterMethodName}' exists. Use a uniquely named wrapper method.");
                match = candidate;
            }

            if (match == null)
                throw new InvalidOperationException($"No public one-parameter setter named '{setterMethodName}' with a supported parameter type was found on {target.GetType().Name}.");
            if (match.ReturnType != typeof(void))
                throw new InvalidOperationException($"Setter '{setterMethodName}' must return void.");

            _cachedSetter = match;
            _cachedParameterType = match.GetParameters()[0].ParameterType;
        }

        private object ConvertValue(string value, Type targetType)
        {
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(bool))
            {
                if (!bool.TryParse(value, out bool booleanValue))
                    throw new FormatException($"'{value}' is not a boolean. Use true or false.");
                return booleanValue;
            }

            if (targetType.IsEnum)
            {
                if (!Enum.TryParse(targetType, value, true, out object enumValue) || !Enum.IsDefined(targetType, enumValue))
                    throw new FormatException($"'{value}' is not a valid {targetType.Name} value.");
                return enumValue;
            }

            CultureInfo culture = useCurrentCulture ? CultureInfo.CurrentCulture : CultureInfo.InvariantCulture;
            return Convert.ChangeType(value, targetType, culture);
        }

        private static bool IsSupportedType(Type type)
        {
            return type == typeof(string) || type == typeof(bool) || type.IsEnum ||
                   type == typeof(byte) || type == typeof(sbyte) ||
                   type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) ||
                   type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        private static string GetUsefulMessage(Exception exception)
        {
            if (exception is TargetInvocationException invocationException && invocationException.InnerException != null)
                return invocationException.InnerException.Message;
            return exception.Message;
        }

        private void ReportFailure(string message)
        {
            Debug.LogWarning($"Could not apply input value: {message}", this);
            onApplyFailed?.Invoke(message);
        }

        private void InvalidateSetterCache()
        {
            _cachedSetter = null;
            _cachedParameterType = null;
        }
    }
}
