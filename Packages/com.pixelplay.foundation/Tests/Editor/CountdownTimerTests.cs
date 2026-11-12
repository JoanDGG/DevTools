using NUnit.Framework;
using PixelPlay.Foundation.Timing;

namespace PixelPlay.Foundation.Tests
{
    public sealed class CountdownTimerTests
    {
        [Test]
        public void Advance_EmitsTicksAndCompletesExactlyOnce()
        {
            var timer = new CountdownTimer(2f);
            int ticks = 0;
            int completions = 0;
            float lastRemaining = -1f;
            timer.Ticked += value => { ticks++; lastRemaining = value; };
            timer.Completed += () => completions++;

            timer.Start();
            timer.Advance(0.75f);
            timer.Advance(2f);
            timer.Advance(1f);

            Assert.That(ticks, Is.EqualTo(2));
            Assert.That(lastRemaining, Is.Zero);
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(timer.IsRunning, Is.False);
        }

        [Test]
        public void PauseAndResume_PreserveRemainingTime()
        {
            var timer = new CountdownTimer(5f);
            timer.Start();
            timer.Advance(1.5f);
            timer.Pause();
            timer.Advance(1f);

            Assert.That(timer.RemainingSeconds, Is.EqualTo(3.5f).Within(0.0001f));

            timer.Resume();
            timer.Advance(0.5f);
            Assert.That(timer.RemainingSeconds, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void Start_WithZeroDuration_CompletesImmediately()
        {
            var timer = new CountdownTimer(0f);
            int completions = 0;
            timer.Completed += () => completions++;

            timer.Start();

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(timer.IsCompleted, Is.True);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(-0.01f)]
        public void Advance_RejectsInvalidDelta(float delta)
        {
            var timer = new CountdownTimer(1f);
            timer.Start();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => timer.Advance(delta));
        }
    }
}
