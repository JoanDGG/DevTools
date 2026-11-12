using System;
using NUnit.Framework;
using PixelPlay.Foundation.Pools;

namespace PixelPlay.Foundation.Tests
{
    public sealed class ObjectPoolTests
    {
        private sealed class PooledItem { }

        [Test]
        public void Constructor_PrewarmCreatesRequestedCountAndLeavesThemInactive()
        {
            int created = 0;
            var pool = new ObjectPool<PooledItem>(() => { created++; return new PooledItem(); }, initialCapacity: 8);

            Assert.That(created, Is.EqualTo(8));
            Assert.That(pool.CountAll, Is.EqualTo(8));
            Assert.That(pool.CountActive, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(8));
            pool.Dispose();
        }

        [Test]
        public void GetAndRelease_ReusesSameInstanceAndTracksCounts()
        {
            int created = 0;
            int acquired = 0;
            int returned = 0;
            var pool = new ObjectPool<PooledItem>(() => { created++; return new PooledItem(); },
                _ => acquired++, _ => returned++);

            PooledItem first = pool.Get();
            Assert.That(pool.Release(first), Is.True);
            PooledItem second = pool.Get();

            Assert.That(second, Is.SameAs(first));
            Assert.That(created, Is.EqualTo(1));
            Assert.That(acquired, Is.EqualTo(2));
            Assert.That(returned, Is.EqualTo(1));
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);
            pool.Dispose();
        }

        [Test]
        public void Release_RejectsDuplicateAndUnownedInstances()
        {
            var pool = new ObjectPool<PooledItem>(() => new PooledItem());
            PooledItem owned = pool.Get();
            var unowned = new PooledItem();

            Assert.That(pool.Release(unowned), Is.False);
            Assert.That(pool.Release(owned), Is.True);
            Assert.That(pool.Release(owned), Is.False);
            pool.Dispose();
        }

        [Test]
        public void Release_WhenCapacityIsFull_DestroysOverflowInstance()
        {
            int destroyed = 0;
            var pool = new ObjectPool<PooledItem>(() => new PooledItem(), onDestroy: _ => destroyed++, maxRetained: 1);
            PooledItem retained = pool.Get();
            PooledItem overflow = pool.Get();

            pool.Release(retained);
            pool.Release(overflow);

            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(pool.CountAll, Is.EqualTo(1));
            Assert.That(destroyed, Is.EqualTo(1));
            pool.Dispose();
        }

        [Test]
        public void ReleaseAll_ReturnsEveryActiveInstance_AndDisposeDestroysOwnedItems()
        {
            int destroyed = 0;
            var pool = new ObjectPool<PooledItem>(() => new PooledItem(), onDestroy: _ => destroyed++, initialCapacity: 2);
            pool.Get();
            pool.Get();

            pool.ReleaseAll();
            Assert.That(pool.CountActive, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(2));

            pool.Dispose();
            Assert.That(destroyed, Is.EqualTo(2));
            Assert.Throws<ObjectDisposedException>(() => pool.Get());
        }

        [Test]
        public void Constructor_RejectsInvalidCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ObjectPool<PooledItem>(() => new PooledItem(), maxRetained: 0));
            Assert.Throws<ArgumentException>(() => new ObjectPool<PooledItem>(() => new PooledItem(), initialCapacity: 3, maxRetained: 2));
        }
    }
}
