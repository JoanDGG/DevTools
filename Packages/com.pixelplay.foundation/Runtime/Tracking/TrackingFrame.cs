using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PixelPlay.Foundation.Tracking
{
    /// <summary>An immutable snapshot of tracking results for one source image frame.</summary>
    public sealed class TrackingFrame
    {
        private readonly ReadOnlyCollection<TrackedPerson> _persons;

        /// <summary>Creates a tracking snapshot for one source image frame.</summary>
        /// <param name="frameId">Provider's frame identifier.</param>
        /// <param name="timestampSeconds">Capture time in seconds using the provider's time origin.</param>
        /// <param name="imageWidth">Source image width in pixels; must be positive.</param>
        /// <param name="imageHeight">Source image height in pixels; must be positive.</param>
        /// <param name="persons">Detected people; null is treated as an empty collection.</param>
        public TrackingFrame(long frameId, double timestampSeconds, int imageWidth, int imageHeight,
            IEnumerable<TrackedPerson> persons)
        {
            if (double.IsNaN(timestampSeconds) || double.IsInfinity(timestampSeconds) || timestampSeconds < 0d)
                throw new ArgumentOutOfRangeException(nameof(timestampSeconds), "Timestamp must be a finite non-negative value.");
            if (imageWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(imageWidth), "Image width must be greater than zero.");
            if (imageHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(imageHeight), "Image height must be greater than zero.");

            var copy = persons == null ? new List<TrackedPerson>() : new List<TrackedPerson>(persons);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] == null)
                    throw new ArgumentException("Person collection must not contain null entries.", nameof(persons));
                if (!ids.Add(copy[i].Id))
                    throw new ArgumentException($"Duplicate tracked person ID '{copy[i].Id}'.", nameof(persons));
            }

            FrameId = frameId;
            TimestampSeconds = timestampSeconds;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
            _persons = copy.AsReadOnly();
        }

        /// <summary>Gets the provider's monotonically increasing frame identifier.</summary>
        public long FrameId { get; }

        /// <summary>Gets the capture timestamp in seconds, using a provider-defined time origin.</summary>
        public double TimestampSeconds { get; }

        /// <summary>Gets the source image width in pixels.</summary>
        public int ImageWidth { get; }

        /// <summary>Gets the source image height in pixels.</summary>
        public int ImageHeight { get; }

        /// <summary>Gets the tracked people in this frame; an empty list means none were detected.</summary>
        public IReadOnlyList<TrackedPerson> Persons => _persons;
    }
}
