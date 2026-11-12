using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PixelPlay.Foundation.Pools
{
    /// <summary>Reuses reference-type instances created by a caller-supplied factory.</summary>
    /// <remarks>Pool operations are synchronous and intended for one thread.</remarks>
    public sealed class ObjectPool<T> : IDisposable where T : class
    {
        private sealed class ReferenceComparer : IEqualityComparer<T>
        {
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T value) => RuntimeHelpers.GetHashCode(value);
        }

        private readonly Func<T> _create;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly Action<T> _onDestroy;
        private readonly Stack<T> _inactive = new Stack<T>();
        private readonly HashSet<T> _active = new HashSet<T>(new ReferenceComparer());
        private readonly HashSet<T> _all = new HashSet<T>(new ReferenceComparer());
        private bool _disposed;

        /// <summary>Creates a pool and optionally prewarms the requested number of instances.</summary>
        /// <param name="create">Factory that returns a new instance whenever the pool is empty.</param>
        /// <param name="onGet">Optional callback invoked before an instance is handed to its caller.</param>
        /// <param name="onRelease">Optional callback invoked before an instance is retained or destroyed.</param>
        /// <param name="onDestroy">Optional callback used to dispose of an instance the pool no longer owns.</param>
        /// <param name="initialCapacity">Number of inactive instances created immediately.</param>
        /// <param name="maxRetained">Maximum number of inactive instances kept for reuse.</param>
        /// <exception cref="ArgumentNullException">The factory is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">A capacity is negative or the retained capacity is less than one.</exception>
        /// <exception cref="ArgumentException">Initial capacity exceeds the retained capacity.</exception>
        public ObjectPool(Func<T> create, Action<T> onGet = null, Action<T> onRelease = null,
            Action<T> onDestroy = null, int initialCapacity = 0, int maxRetained = 100)
        {
            _create = create ?? throw new ArgumentNullException(nameof(create));
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            if (maxRetained < 1)
                throw new ArgumentOutOfRangeException(nameof(maxRetained), "At least one inactive instance must be retainable.");
            if (initialCapacity > maxRetained)
                throw new ArgumentException("Initial capacity cannot exceed the maximum retained count.", nameof(initialCapacity));

            _onGet = onGet;
            _onRelease = onRelease;
            _onDestroy = onDestroy;
            MaxRetained = maxRetained;
            Prewarm(initialCapacity);
        }

        /// <summary>Gets the maximum number of inactive instances retained by the pool.</summary>
        public int MaxRetained { get; }

        /// <summary>Gets the total number of instances currently owned by the pool.</summary>
        public int CountAll => _all.Count;

        /// <summary>Gets the number of instances currently checked out.</summary>
        public int CountActive => _active.Count;

        /// <summary>Gets the number of reusable inactive instances.</summary>
        public int CountInactive => _inactive.Count;

        /// <summary>Gets an instance, creating one when no inactive instance is available.</summary>
        /// <returns>The checked-out instance.</returns>
        public T Get()
        {
            ThrowIfDisposed();
            T item;
            if (_inactive.Count > 0)
            {
                item = _inactive.Pop();
            }
            else
            {
                item = _create();
                if (item == null)
                    throw new InvalidOperationException("The object pool factory returned null.");
                if (!_all.Add(item))
                    throw new InvalidOperationException("The object pool factory returned an instance already owned by this pool.");
            }

            _active.Add(item);
            try
            {
                _onGet?.Invoke(item);
            }
            catch
            {
                _active.Remove(item);
                DestroyItem(item);
                throw;
            }

            return item;
        }

        /// <summary>Returns an active instance; returns false if the instance is not currently checked out.</summary>
        /// <param name="item">Instance previously returned by <see cref="Get"/>.</param>
        /// <returns>True when the active instance was returned to the pool.</returns>
        public bool Release(T item)
        {
            ThrowIfDisposed();
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (!_active.Remove(item))
                return false;

            try
            {
                _onRelease?.Invoke(item);
            }
            catch
            {
                DestroyItem(item);
                throw;
            }

            if (_inactive.Count >= MaxRetained)
                DestroyItem(item);
            else
                _inactive.Push(item);
            return true;
        }

        /// <summary>Creates inactive instances until the total owned count reaches the requested count.</summary>
        /// <param name="count">Target total count, including currently checked-out objects.</param>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            if (count < 0 || count > MaxRetained)
                throw new ArgumentOutOfRangeException(nameof(count), "Prewarm count must fit within the pool's retained capacity.");

            var acquired = new List<T>(Math.Max(0, count - _all.Count));
            try
            {
                // Keep each new object checked out until creation is complete. Returning it
                // inside this loop would let the next Get reuse it forever without growing _all.
                while (_all.Count < count)
                    acquired.Add(Get());
            }
            finally
            {
                for (int i = acquired.Count - 1; i >= 0; i--)
                {
                    if (_active.Contains(acquired[i]))
                        Release(acquired[i]);
                }
            }
        }

        /// <summary>Returns every checked-out instance to the pool.</summary>
        public void ReleaseAll()
        {
            ThrowIfDisposed();
            T[] snapshot = new T[_active.Count];
            _active.CopyTo(snapshot);
            for (int i = 0; i < snapshot.Length; i++)
                Release(snapshot[i]);
        }

        /// <summary>Destroys only inactive instances while leaving checked-out instances owned by the pool.</summary>
        public void ClearInactive()
        {
            ThrowIfDisposed();
            while (_inactive.Count > 0)
                DestroyItem(_inactive.Pop());
        }

        /// <summary>Destroys all owned instances and prevents further use.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            T[] snapshot = new T[_all.Count];
            _all.CopyTo(snapshot);
            _active.Clear();
            _inactive.Clear();
            _all.Clear();
            for (int i = 0; i < snapshot.Length; i++)
                _onDestroy?.Invoke(snapshot[i]);
        }

        private void DestroyItem(T item)
        {
            _active.Remove(item);
            _all.Remove(item);
            _onDestroy?.Invoke(item);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
        }
    }
}
