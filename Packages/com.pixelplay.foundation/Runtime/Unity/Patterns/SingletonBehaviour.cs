using UnityEngine;

namespace PixelPlay.Foundation.Unity.Patterns
{
    /// <summary>Base class for a scene-level MonoBehaviour singleton.</summary>
    /// <typeparam name="T">The concrete component type that inherits from this class.</typeparam>
    /// <remarks>
    /// Inherit as <c>public sealed class GameServices : SingletonBehaviour&lt;GameServices&gt;</c>.
    /// The first active instance owns <see cref="Instance"/>; duplicate GameObjects are destroyed.
    /// This base class does not persist its instance across scene loads.
    /// If overriding <see cref="Awake"/>, call <c>base.Awake()</c> first, then return when
    /// <see cref="Instance"/> is not this component.
    /// </remarks>
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        /// <summary>Gets the active singleton instance, or null when none is present.</summary>
        public static T Instance { get; private set; }

        /// <summary>Registers this component as the singleton or destroys its duplicate.</summary>
        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
        }

        /// <summary>Clears the static reference when the owning component is destroyed.</summary>
        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
