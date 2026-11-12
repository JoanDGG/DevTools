using System;
using System.Collections.Generic;
using PixelPlay.Foundation.Pools;
using UnityEngine;

namespace PixelPlay.Foundation.Unity.Pools
{
    /// <summary>Inspector-friendly pool for reusing prefab instances such as projectiles and effects.</summary>
    public sealed class GameObjectPool : MonoBehaviour
    {
        [SerializeField, Tooltip("Prefab asset to create and reuse; it does not need a scene instance.")] private GameObject prefab;
        [SerializeField, Min(0), Tooltip("Number of inactive instances created at initialization. Must not exceed Max Retained.")] private int prewarmCount = 8;
        [SerializeField, Min(1), Tooltip("Maximum inactive instances kept for reuse; extras are destroyed when returned.")] private int maxRetained = 64;
        [SerializeField, Min(0f), Tooltip("Default lifetime in seconds for acquired objects; zero keeps them active until manually released.")] private float defaultLifetimeSeconds;

        [SerializeField, Tooltip("Optional transform used as a convenient world-space spawn location and orientation.")] private Transform spawnPoint;

        private struct TimedInstance
        {
            public GameObject Instance;
            public float ExpiresAt;
        }

        private Transform _inactiveRoot;
        private Vector3 _prefabLocalScale;
        private ObjectPool<GameObject> _pool;
        private readonly List<TimedInstance> _timedInstances = new List<TimedInstance>();
        private Dictionary<GameObject, int> _timedInstanceIndices = new Dictionary<GameObject, int>();

        /// <summary>Gets the number of objects currently checked out.</summary>
        public int CountActive => _pool == null ? 0 : _pool.CountActive;

        /// <summary>Gets the number of objects waiting in the pool.</summary>
        public int CountInactive => _pool == null ? 0 : _pool.CountInactive;

        /// <summary>Gets the default lifetime in seconds; zero disables automatic release.</summary>
        public float DefaultLifetimeSeconds => defaultLifetimeSeconds;

        /// <summary>Gets the optional configured spawn point.</summary>
        public Transform SpawnPoint => spawnPoint;

        /// <summary>Sets the optional spawn point.</summary>
        public void SetSpawnPoint(Transform value) => spawnPoint = value;

        /// <summary>Sets the default lifetime used by subsequent calls to <see cref="Acquire"/>.</summary>
        public void SetDefaultLifetimeSeconds(float value)
        {
            ValidateLifetime(value);
            defaultLifetimeSeconds = value;
        }

        /// <summary>Sets the prefab before the pool is initialized.</summary>
        public void SetPrefab(GameObject value)
        {
            EnsureNotInitialized();
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            prefab = value;
        }

        /// <summary>Sets the number of instances created when the pool initializes.</summary>
        public void SetPrewarmCount(int value)
        {
            EnsureNotInitialized();
            if (value < 0 || value > maxRetained)
                throw new ArgumentOutOfRangeException(nameof(value), "Prewarm count must be between zero and Max Retained.");
            prewarmCount = value;
        }

        /// <summary>Sets the maximum number of inactive instances retained by the pool.</summary>
        public void SetMaxRetained(int value)
        {
            EnsureNotInitialized();
            if (value < 1 || value < prewarmCount)
                throw new ArgumentOutOfRangeException(nameof(value), "Max Retained must be positive and at least the prewarm count.");
            maxRetained = value;
        }

        private void EnsureNotInitialized()
        {
            if (_pool != null)
                throw new InvalidOperationException("Pool configuration cannot change after initialization.");
        }

        /// <summary>Creates the inactive pool root and prewarms configured instances.</summary>
        public void Initialize()
        {
            if (_pool != null)
                return;
            if (prefab == null)
                throw new InvalidOperationException("Assign a prefab before initializing the pool.");
            if (prewarmCount < 0)
                throw new InvalidOperationException("Prewarm count cannot be negative.");
            if (maxRetained < 1 || prewarmCount > maxRetained)
                throw new InvalidOperationException("Maximum retained count must be positive and at least the prewarm count.");
            if (_timedInstances.Capacity < prewarmCount)
                _timedInstances.Capacity = prewarmCount;
            _timedInstanceIndices = new Dictionary<GameObject, int>(prewarmCount);

            _prefabLocalScale = prefab.transform.localScale;
            var inactiveRootObject = new GameObject($"{name} Pool Inactive");
            inactiveRootObject.transform.SetParent(transform, false);
            inactiveRootObject.SetActive(false);
            _inactiveRoot = inactiveRootObject.transform;

            try
            {
                _pool = new ObjectPool<GameObject>(CreateInstance, onRelease: ReturnToInactiveRoot,
                    onDestroy: DestroyInstance, initialCapacity: prewarmCount, maxRetained: maxRetained);
            }
            catch
            {
                DestroyObject(inactiveRootObject);
                _inactiveRoot = null;
                throw;
            }
        }

        /// <summary>Gets an instance, sets its world pose while inactive, activates it, and applies the configured default lifetime.</summary>
        /// <param name="position">World position assigned before activation.</param>
        /// <param name="rotation">World rotation assigned before activation.</param>
        /// <param name="parent">Optional active parent; defaults to this pool's transform.</param>
        /// <returns>The activated pooled instance.</returns>
        public GameObject Acquire(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return AcquireWithLifetime(position, rotation, defaultLifetimeSeconds, parent);
        }

        /// <summary>Gets an instance and returns it to the pool after the given lifetime.</summary>
        /// <param name="position">World position assigned before activation.</param>
        /// <param name="rotation">World rotation assigned before activation.</param>
        /// <param name="lifetimeSeconds">Lifetime in scaled game seconds; zero disables timed release.</param>
        /// <param name="parent">Optional active parent; defaults to this pool's transform.</param>
        /// <returns>The activated pooled instance.</returns>
        public GameObject AcquireWithLifetime(Vector3 position, Quaternion rotation, float lifetimeSeconds, Transform parent = null)
        {
            ValidateLifetime(lifetimeSeconds);
            EnsureInitialized();
            GameObject instance = _pool.Get();
            Transform instanceTransform = instance.transform;
            instanceTransform.SetParent(parent == null ? transform : parent, false);
            instanceTransform.SetPositionAndRotation(position, rotation);
            instanceTransform.localScale = _prefabLocalScale;
            instance.SetActive(true);
            if (lifetimeSeconds > 0f)
                AddTimedInstance(instance, Time.time + lifetimeSeconds);
            return instance;
        }

        /// <summary>Acquires an instance at the configured spawn point, or this pool's transform if none is assigned.</summary>
        /// <param name="parent">Optional active parent; defaults to this pool's transform.</param>
        public GameObject AcquireAtSpawnPoint(Transform parent = null)
        {
            Transform point = spawnPoint != null ? spawnPoint : transform;
            return Acquire(point.position, point.rotation, parent);
        }

        /// <summary>Acquires an instance at a local offset from the configured spawn point.</summary>
        /// <param name="localOffset">Offset in the spawn point's local space.</param>
        /// <param name="parent">Optional active parent; defaults to this pool's transform.</param>
        public GameObject AcquireAtSpawnPoint(Vector3 localOffset, Transform parent = null)
        {
            Transform point = spawnPoint != null ? spawnPoint : transform;
            return Acquire(point.TransformPoint(localOffset), point.rotation, parent);
        }

        /// <summary>Returns an acquired instance; returns false for objects not currently owned as active.</summary>
        /// <param name="instance">Object previously returned by <see cref="Acquire"/>.</param>
        /// <returns>True if the instance was returned.</returns>
        public bool Release(GameObject instance)
        {
            EnsureInitialized();
            RemoveTimedInstance(instance);
            return _pool.Release(instance);
        }

        /// <summary>Returns every checked-out instance to the pool.</summary>
        public void ReleaseAll()
        {
            EnsureInitialized();
            _timedInstances.Clear();
            _timedInstanceIndices.Clear();
            _pool.ReleaseAll();
        }

        /// <summary>Destroys inactive pooled instances while retaining active instances.</summary>
        public void ClearInactive()
        {
            EnsureInitialized();
            _pool.ClearInactive();
        }

        private GameObject CreateInstance()
        {
            GameObject instance = Instantiate(prefab, _inactiveRoot, false);
            instance.SetActive(false);
            return instance;
        }

        private void ReturnToInactiveRoot(GameObject instance)
        {
            if (instance == null)
                return;
            instance.SetActive(false);
            instance.transform.SetParent(_inactiveRoot, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = _prefabLocalScale;
        }

        private void DestroyInstance(GameObject instance)
        {
            if (instance != null)
                DestroyObject(instance);
        }

        private void EnsureInitialized()
        {
            if (_pool == null)
                Initialize();
        }

        private static void ValidateLifetime(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                throw new ArgumentOutOfRangeException(nameof(value), "Lifetime must be finite and non-negative.");
        }

        private void Update()
        {
            if (_pool == null)
                return;

            while (_timedInstances.Count > 0 && Time.time >= _timedInstances[0].ExpiresAt)
            {
                GameObject instance = PopEarliestTimedInstance();
                if (instance != null)
                    _pool.Release(instance);
            }
        }

        private void AddTimedInstance(GameObject instance, float expiresAt)
        {
            int index = _timedInstances.Count;
            _timedInstances.Add(new TimedInstance { Instance = instance, ExpiresAt = expiresAt });
            _timedInstanceIndices[instance] = index;
            SiftTimedInstanceUp(index);
        }

        private void RemoveTimedInstance(GameObject instance)
        {
            if (ReferenceEquals(instance, null) || !_timedInstanceIndices.TryGetValue(instance, out int index))
                return;

            _timedInstanceIndices.Remove(instance);
            int lastIndex = _timedInstances.Count - 1;
            if (index == lastIndex)
            {
                _timedInstances.RemoveAt(lastIndex);
                return;
            }

            TimedInstance moved = _timedInstances[lastIndex];
            _timedInstances[index] = moved;
            _timedInstances.RemoveAt(lastIndex);
            _timedInstanceIndices[moved.Instance] = index;
            RestoreTimedInstanceHeap(index);
        }

        private GameObject PopEarliestTimedInstance()
        {
            GameObject earliest = _timedInstances[0].Instance;
            _timedInstanceIndices.Remove(earliest);
            int lastIndex = _timedInstances.Count - 1;
            if (lastIndex == 0)
            {
                _timedInstances.RemoveAt(0);
                return earliest;
            }

            TimedInstance moved = _timedInstances[lastIndex];
            _timedInstances[0] = moved;
            _timedInstances.RemoveAt(lastIndex);
            _timedInstanceIndices[moved.Instance] = 0;
            SiftTimedInstanceDown(0);
            return earliest;
        }

        private void RestoreTimedInstanceHeap(int index)
        {
            int parent = (index - 1) / 2;
            if (index > 0 && _timedInstances[index].ExpiresAt < _timedInstances[parent].ExpiresAt)
                SiftTimedInstanceUp(index);
            else
                SiftTimedInstanceDown(index);
        }

        private void SiftTimedInstanceUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_timedInstances[parent].ExpiresAt <= _timedInstances[index].ExpiresAt)
                    break;
                SwapTimedInstances(parent, index);
                index = parent;
            }
        }

        private void SiftTimedInstanceDown(int index)
        {
            while (true)
            {
                int left = index * 2 + 1;
                if (left >= _timedInstances.Count)
                    return;
                int right = left + 1;
                int earliestChild = right < _timedInstances.Count &&
                                    _timedInstances[right].ExpiresAt < _timedInstances[left].ExpiresAt
                    ? right
                    : left;
                if (_timedInstances[index].ExpiresAt <= _timedInstances[earliestChild].ExpiresAt)
                    return;
                SwapTimedInstances(index, earliestChild);
                index = earliestChild;
            }
        }

        private void SwapTimedInstances(int first, int second)
        {
            TimedInstance temporary = _timedInstances[first];
            _timedInstances[first] = _timedInstances[second];
            _timedInstances[second] = temporary;
            _timedInstanceIndices[_timedInstances[first].Instance] = first;
            _timedInstanceIndices[_timedInstances[second].Instance] = second;
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
            _pool = null;
            _timedInstances.Clear();
            _timedInstanceIndices.Clear();
            if (_inactiveRoot != null)
                DestroyObject(_inactiveRoot.gameObject);
            _inactiveRoot = null;
        }
    }
}
