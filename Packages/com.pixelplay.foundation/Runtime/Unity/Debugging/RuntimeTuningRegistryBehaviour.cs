using System;
using PixelPlay.Foundation.Debugging;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Debugging
{
    /// <summary>Builds a project's runtime tuning registry from Inspector definitions.</summary>
    public sealed class RuntimeTuningRegistryBehaviour : MonoBehaviour
    {
        public enum ValueKind
        {
            Float,
            Integer,
            Boolean
        }

        [Serializable]
        private sealed class TuningDefinition
        {
            public string id;
            public string displayName;
            public string category = "General";
            [TextArea] public string description;
            public ValueKind kind;
            public float floatDefault;
            public float floatMinimum;
            public float floatMaximum = 1f;
            public int integerDefault;
            public int integerMinimum;
            public int integerMaximum = 10;
            public bool booleanDefault;
        }

        [SerializeField] private TuningDefinition[] definitions = Array.Empty<TuningDefinition>();

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
