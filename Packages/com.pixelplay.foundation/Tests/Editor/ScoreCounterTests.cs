using NUnit.Framework;
using PixelPlay.Foundation.Scoring;

namespace PixelPlay.Foundation.Tests
{
    public sealed class ScoreCounterTests
    {
        [Test]
        public void Changes_ReportPreviousAndNewValues_AndResetToStartingScore()
        {
            var score = new ScoreCounter(10);
            int eventCount = 0;
            int previous = 0;
            int current = 0;
            score.Changed += (oldValue, newValue) =>
            {
                eventCount++;
                previous = oldValue;
                current = newValue;
            };

            score.Add(5);
            score.Subtract(2);
            score.Reset();

            Assert.That(score.Value, Is.EqualTo(10));
            Assert.That(eventCount, Is.EqualTo(3));
            Assert.That(previous, Is.EqualTo(13));
            Assert.That(current, Is.EqualTo(10));
        }

        [Test]
        public void SettingSameValue_DoesNotRaiseChanged()
        {
            var score = new ScoreCounter(7);
            int eventCount = 0;
            score.Changed += (_, __) => eventCount++;

            score.SetScore(7);

            Assert.That(eventCount, Is.Zero);
        }

        [Test]
        public void Add_ThrowsInsteadOfOverflowing()
        {
            var score = new ScoreCounter(int.MaxValue);
            Assert.Throws<System.OverflowException>(() => score.Add(1));
            Assert.That(score.Value, Is.EqualTo(int.MaxValue));
        }
    }
}
