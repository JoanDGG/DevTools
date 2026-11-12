using System;
using PixelPlay.Foundation.Debugging;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Debugging
{
    /// <summary>Builds a project's runtime tuning registry from Inspector definitions.</summary>
    public sealed class RuntimeTuningRegistryBehaviour : MonoBehaviour
    {
        /// <summary>Data type used by an Inspector-defined tuning value.</summary>
        public enum ValueKind
        {
            /// <summary>Bounded floating-point setting.</summary>
            Float,
            /// <summary>Bounded integer setting.</summary>
            Integer,
            /// <summary>Boolean setting.</summary>
            Boolean
        }

        [Serializable]
        private sealed class TuningDefinition
        {
            [Tooltip("Stable ID used by game code to retrieve this value. Must be unique in the registry.")] public string id;
            [Tooltip("Readable label displayed in the runtime debug panel.")] public string displayName;
            [Tooltip("Category used to group values in development tools.")] public string category = "General";
            [TextArea, Tooltip("Optional explanation shown with this setting.")] public string description;
            [Tooltip("Selects which default and range fields are used.")] public ValueKind kind;
            [Tooltip("Initial float value; must be within the minimum and maximum.")] public float floatDefault;
            [Tooltip("Lowest accepted float value.")] public float floatMinimum;
            [Tooltip("Highest accepted float value.")] public float floatMaximum = 1f;
            [Tooltip("Initial integer value; must be within the minimum and maximum.")] public int integerDefault;
            [Tooltip("Lowest accepted integer value.")] public int integerMinimum;
            [Tooltip("Highest accepted integer value.")] public int integerMaximum = 10;
            [Tooltip("Initial boolean value.")] public bool booleanDefault;
        }

        [SerializeField, Tooltip("Project-specific values to register for runtime gameplay tuning.")] private TuningDefinition[] definitions = Array.Empty<TuningDefinition>();

        private RuntimeTuningRegistry _registry;

        /// <summary>Gets the registry, building it from the serialized definitions on first access.</summary>
        public RuntimeTuningRegistry Registry
        {
            get
            {
                if (_registry == null)
                    BuildRegistry();
                return _registry;
            }
        }

        private void Awake() => BuildRegistry();

        /// <summary>Rebuilds the runtime registry from the current Inspector definitions.</summary>
        private void BuildRegistry()
        {
            var registry = new RuntimeTuningRegistry();
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Length; i++)
                {
                    TuningDefinition definition = definitions[i];
                    if (definition == null)
                        continue;

                    try
                    {
                        switch (definition.kind)
                        {
                            case ValueKind.Float:
                                registry.AddFloat(definition.id, definition.displayName, definition.floatDefault,
                                    definition.floatMinimum, definition.floatMaximum, definition.category, definition.description);
                                break;
                            case ValueKind.Integer:
                                registry.AddInteger(definition.id, definition.displayName, definition.integerDefault,
                                    definition.integerMinimum, definition.integerMaximum, definition.category, definition.description);
                                break;
                            case ValueKind.Boolean:
                                registry.AddBoolean(definition.id, definition.displayName, definition.booleanDefault,
                                    definition.category, definition.description);
                                break;
                            default:
                                Debug.LogError($"Unknown runtime tuning type at definition index {i}.", this);
                                break;
                        }
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"Could not register runtime tuning value at index {i}: {exception.Message}", this);
                    }
                }
            }

            _registry = registry;
        }
    }
}
