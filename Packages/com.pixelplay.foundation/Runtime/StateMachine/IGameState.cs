namespace PixelPlay.Foundation.StateMachine
{
    /// <summary>Defines a state with explicit entry, exit, and update lifecycle methods.</summary>
    public interface IGameState
    {
        /// <summary>Called when this state becomes active.</summary>
        void Enter();

        /// <summary>Called when this state stops being active.</summary>
        void Exit();

        /// <summary>Called by the owner once per update while this state is active.</summary>
        void Tick(float deltaTime);
    }
}
