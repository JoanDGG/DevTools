using System;
using System.Collections.Generic;

namespace PixelPlay.Foundation.Events
{
    /// <summary>A lightweight, type-safe event bus for use from a single thread.</summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, IChannel> _channels = new Dictionary<Type, IChannel>();

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Channel<TEvent> channel = GetOrCreateChannel<TEvent>();
            return channel.Subscribe(handler);
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent eventData)
        {
            if (_channels.TryGetValue(typeof(TEvent), out IChannel channel))
                ((Channel<TEvent>)channel).Publish(eventData);
        }

        /// <inheritdoc />
        public void Clear()
        {
            foreach (IChannel channel in _channels.Values)
                channel.Clear();

            _channels.Clear();
        }

        private Channel<TEvent> GetOrCreateChannel<TEvent>()
        {
            Type eventType = typeof(TEvent);
            if (_channels.TryGetValue(eventType, out IChannel existing))
                return (Channel<TEvent>)existing;

            var channel = new Channel<TEvent>();
            _channels.Add(eventType, channel);
            return channel;
        }

        private interface IChannel
        {
            void Clear();
        }

        private sealed class Channel<TEvent> : IChannel
        {
            private readonly List<Subscription<TEvent>> _subscriptions = new List<Subscription<TEvent>>();

            public IDisposable Subscribe(Action<TEvent> handler)
            {
                var subscription = new Subscription<TEvent>(handler, Remove);
                _subscriptions.Add(subscription);
                return subscription;
            }

            public void Publish(TEvent eventData)
            {
                Subscription<TEvent>[] snapshot = _subscriptions.ToArray();
                foreach (Subscription<TEvent> subscription in snapshot)
                {
                    if (subscription.IsActive)
                        subscription.Invoke(eventData);
                }
            }

            public void Clear()
            {
                Subscription<TEvent>[] snapshot = _subscriptions.ToArray();
                foreach (Subscription<TEvent> subscription in snapshot)
                    subscription.Dispose();
            }

            private void Remove(Subscription<TEvent> subscription)
            {
                _subscriptions.Remove(subscription);
            }
        }

        private sealed class Subscription<TEvent> : IDisposable
        {
            private Action<TEvent> _handler;
            private Action<Subscription<TEvent>> _remove;

            public Subscription(Action<TEvent> handler, Action<Subscription<TEvent>> remove)
            {
                _handler = handler;
                _remove = remove;
                IsActive = true;
            }

            public bool IsActive { get; private set; }

            public void Invoke(TEvent eventData) => _handler?.Invoke(eventData);

            public void Dispose()
            {
                if (!IsActive)
                    return;

                IsActive = false;
                Action<Subscription<TEvent>> remove = _remove;
                _handler = null;
                _remove = null;
                remove?.Invoke(this);
            }
        }
    }
}
