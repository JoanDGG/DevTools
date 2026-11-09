namespace PixelPlay.Foundation
{
    /// <summary>Represents a system that can return to its initial state.</summary>
    public interface IResettable
    {
        /// <summary>Restores the system's initial state.</summary>
        void Reset();
    }
}
