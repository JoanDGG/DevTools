using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PixelPlay.Foundation.Tracking
{
    /// <summary>An entity detected in a tracking frame and its available landmarks.</summary>
    public sealed class TrackedPerson
    {
        private readonly ReadOnlyCollection<TrackedLandmark> _landmarks;

        public TrackedPerson(string id, IEnumerable<TrackedLandmark> landmarks)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Tracked person ID must not be null or whitespace.", nameof(id));
            if (landmarks == null)
                throw new ArgumentNullException(nameof(landmarks));

            var copy = new List<TrackedLandmark>(landmarks);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < copy.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(copy[i].Id))
                    throw new ArgumentException("Landmark collection must not contain uninitialized entries.", nameof(landmarks));
                if (!ids.Add(copy[i].Id))
                    throw new ArgumentException($"Duplicate landmark ID '{copy[i].Id}'.", nameof(landmarks));
            }

            Id = id;
            _landmarks = copy.AsReadOnly();
        }

        /// <summary>Gets the stable ID assigned to this entity by the provider.</summary>
        public string Id { get; }

        /// <summary>Gets the read-only landmarks available for this person in this frame.</summary>
        public IReadOnlyList<TrackedLandmark> Landmarks => _landmarks;

        /// <summary>Finds a landmark by its provider-defined ID using ordinal comparison.</summary>
        public bool TryGetLandmark(string landmarkId, out TrackedLandmark landmark)
        {
            for (int i = 0; i < _landmarks.Count; i++)
            {
                if (string.Equals(_landmarks[i].Id, landmarkId, StringComparison.Ordinal))
                {
                    landmark = _landmarks[i];
                    return true;
                }
            }

            landmark = default;
            return false;
        }
    }
}
