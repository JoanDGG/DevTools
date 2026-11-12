using System;
using NUnit.Framework;
using PixelPlay.Foundation.Events;

namespace PixelPlay.Foundation.Tests
{
    public sealed class EventBusTests
    {
        private readonly struct SampleEvent
        {
            public SampleEvent(int value) => Value = value;
            public int Value { get; }
        }

        [Test]
        public void Publish_NotifiesOnlyMatchingActiveSubscriptions()
        {
            var bus = new EventBus();
            int received = 0;
            IDisposable subscription = bus.Subscribe<SampleEvent>(message => received += message.Value);

            bus.Publish(new SampleEvent(3));
            subscription.Dispose();
            bus.Publish(new SampleEvent(5));

            Assert.That(received, Is.EqualTo(3));
        }

        [Test]
        public void Publish_UsesListenerSnapshotAndSkipsDisposedListeners()
        {
            var bus = new EventBus();
            int firstCount = 0;
            int secondCount = 0;
            IDisposable second = null;
            bool addedLateListener = false;

            bus.Subscribe<SampleEvent>(_ =>
            {
                firstCount++;
                second?.Dispose();
                if (!addedLateListener)
                {
                    addedLateListener = true;
                    bus.Subscribe<SampleEvent>(message => secondCount += message.Value);
                }
            });
            second = bus.Subscribe<SampleEvent>(_ => Assert.Fail("Disposed listener should be skipped."));

            bus.Publish(new SampleEvent(2));
            Assert.That(secondCount, Is.Zero);

            bus.Publish(new SampleEvent(4));
            Assert.That(firstCount, Is.EqualTo(2));
            Assert.That(secondCount, Is.EqualTo(4));
        }

        [Test]
        public void Clear_DeactivatesExistingSubscriptionTokens()
        {
            var bus = new EventBus();
            int received = 0;
            IDisposable subscription = bus.Subscribe<SampleEvent>(_ => received++);

            bus.Clear();
            subscription.Dispose();
            bus.Publish(new SampleEvent(1));

            Assert.That(received, Is.Zero);
        }
    }
}
