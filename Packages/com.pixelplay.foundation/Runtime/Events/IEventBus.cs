using System;

namespace PixelPlay.Foundation.Events
{
    /// <summary>Publishes typed events to registered listeners.</summary>
    /// <remarks>Subscriptions should be disposed when their owning object is no longer active.</remarks>
    public interface IEventBus
    {
        /// <summary>Subscribes to an event type and returns a token that removes the subscription when disposed.</summary>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);

        /// <summary>Publishes an event to the listeners currently subscribed to its type.</summary>
        void Publish<TEvent>(TEvent eventData);

        /// <summary>Removes every subscription and deactivates its subscription token.</summary>
        void Clear();
    }
}
