using System;
using NUnit.Framework;
using PixelPlay.Foundation.StateMachine;

namespace PixelPlay.Foundation.Tests
{
    public sealed class GameStateMachineTests
    {
        private sealed class FakeState : IGameState
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public float TickedSeconds { get; private set; }
            public Action OnEnter { get; set; }

            public void Enter() { EnterCount++; OnEnter?.Invoke(); }
            public void Exit() => ExitCount++;
            public void Tick(float deltaTime) => TickedSeconds += deltaTime;
        }

        [Test]
        public void ChangeState_ExitsOldStateAndEntersNewState()
        {
            var machine = new GameStateMachine();
            var menu = new FakeState();
            var gameplay = new FakeState();

            machine.ChangeState(menu);
            machine.Tick(0.25f);
            machine.ChangeState(gameplay);

            Assert.That(menu.EnterCount, Is.EqualTo(1));
            Assert.That(menu.ExitCount, Is.EqualTo(1));
            Assert.That(menu.TickedSeconds, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(gameplay.EnterCount, Is.EqualTo(1));
            Assert.That(machine.CurrentState, Is.SameAs(gameplay));
        }

        [Test]
        public void Stop_ExitsCurrentStateAndClearsCurrentState()
        {
            var machine = new GameStateMachine();
            var state = new FakeState();
            machine.ChangeState(state);

            machine.Stop();

            Assert.That(state.ExitCount, Is.EqualTo(1));
            Assert.That(machine.CurrentState, Is.Null);
            Assert.That(machine.IsRunning, Is.False);
        }

        [Test]
        public void TransitionFromEnter_IsRejected()
        {
            var machine = new GameStateMachine();
            var nested = new FakeState();
            var first = new FakeState { OnEnter = () => machine.ChangeState(nested) };

            Assert.Throws<InvalidOperationException>(() => machine.ChangeState(first));
        }
    }
}
