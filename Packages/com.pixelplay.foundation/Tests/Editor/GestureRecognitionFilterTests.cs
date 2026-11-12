using System.Collections.Generic;
using NUnit.Framework;
using PixelPlay.Foundation.Gestures;

namespace PixelPlay.Foundation.Tests
{
    public sealed class GestureRecognitionFilterTests
    {
        [Test]
        public void Process_EmitsStartAndEndAfterConfiguredStabilityWindows()
        {
            var filter = new GestureRecognitionFilter(0.7f, 0.4f, 0.1f, 0.15f);
            var changes = new List<GestureStateChange>();
            filter.StateChanged += changes.Add;

            filter.Process(Frame(0, 0.0, new GestureCandidate("left", "pinch", 0.9f)));
            Assert.That(changes, Is.Empty);
            filter.Process(Frame(1, 0.11, new GestureCandidate("left", "pinch", 0.85f)));
            filter.Process(Frame(2, 0.2));
            filter.Process(Frame(3, 0.36));

            Assert.That(changes, Has.Count.EqualTo(2));
            Assert.That(changes[0].Kind, Is.EqualTo(GestureStateChange.ChangeKind.Started));
            Assert.That(changes[0].GestureId, Is.EqualTo("pinch"));
            Assert.That(changes[1].Kind, Is.EqualTo(GestureStateChange.ChangeKind.Ended));
        }

        private static GestureFrame Frame(long id, double time, params GestureCandidate[] candidates) =>
            new GestureFrame(id, time, candidates);
    }
}
