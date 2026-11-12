using PixelPlay.Foundation.Unity.Pools;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Demonstrates acquiring and returning prefab instances through a GameObjectPool.</summary>
    public sealed class ObjectPoolQuickStart : MonoBehaviour
    {
        [SerializeField, Tooltip("Pool used to create and recycle the sample objects.")] private GameObjectPool pool;
        [SerializeField, Tooltip("World-space distance between objects spawned by the context action.")] private float spawnSpacing = 1f;
        [SerializeField, Tooltip("Initialize the assigned pool in Start instead of waiting for the first spawn.")] private bool initializePoolOnStart;

        private int _spawnCount;
        private bool _hasStarted;

        /// <summary>Sets the pool used by subsequent sample spawn and release operations.</summary>
        public void SetPool(GameObjectPool value)
        {
            pool = value;
            if (_hasStarted && initializePoolOnStart && pool != null)
                pool.Initialize();
        }

        /// <summary>Sets whether the assigned pool is initialized in Start.</summary>
        public void SetInitializePoolOnStart(bool value)
        {
            initializePoolOnStart = value;
            if (_hasStarted && value && pool != null)
                pool.Initialize();
        }

        /// <summary>Sets the spacing used for subsequently spawned sample objects.</summary>
        public void SetSpawnSpacing(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new System.ArgumentOutOfRangeException(nameof(value), "Spawn spacing must be finite.");
            spawnSpacing = value;
        }

        private void Start()
        {
            _hasStarted = true;
            if (!initializePoolOnStart)
                return;
            if (pool == null)
            {
                Debug.LogError("Initialize Pool On Start is enabled, but no GameObjectPool is assigned.", this);
                return;
            }

            pool.Initialize();
        }

        [ContextMenu("Spawn Pooled Object")]
        public void SpawnPooledObject()
        {
            if (pool == null)
            {
                Debug.LogError("Assign a GameObjectPool before spawning.", this);
                return;
            }

            Vector3 localOffset = Vector3.right * (spawnSpacing * _spawnCount);
            if (pool.SpawnPoint != null)
                pool.AcquireAtSpawnPoint(localOffset);
            else
                pool.Acquire(transform.position + transform.right * (spawnSpacing * _spawnCount), transform.rotation);
            _spawnCount++;
        }

        [ContextMenu("Release All Spawned Objects")]
        public void ReleaseSpawnedObjects()
        {
            if (pool == null)
                return;
            pool.ReleaseAll();
            _spawnCount = 0;
        }
    }
}
