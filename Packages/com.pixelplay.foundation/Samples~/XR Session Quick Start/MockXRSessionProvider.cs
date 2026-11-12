using System;
using System.Threading;
using System.Threading.Tasks;
using PixelPlay.Foundation.XR;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>A small asynchronous mock that demonstrates an XR provider lifecycle.</summary>
    public sealed class MockXRSessionProvider : IXRSessionProvider
    {
        /// <inheritdoc />
        public XRSessionState State { get; private set; } = XRSessionState.Ready;
        /// <inheritdoc />
        public XRSessionMode Mode => XRSessionMode.MixedReality;

        /// <inheritdoc />
        public event Action<XRSessionState> StateChanged;
        /// <inheritdoc />
        public event Action<XRSessionError> ErrorReported;

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (State != XRSessionState.Ready)
                throw new InvalidOperationException($"Cannot start while provider state is {State}.");
            SetState(XRSessionState.Starting);
            try
            {
                await Task.Delay(150, cancellationToken);
                SetState(XRSessionState.Running);
            }
            catch (OperationCanceledException)
            {
                SetState(XRSessionState.Ready);
                throw;
            }
        }

        /// <inheritdoc />
        public Task PauseAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequireState(XRSessionState.Running, "pause");
            SetState(XRSessionState.Paused);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ResumeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequireState(XRSessionState.Paused, "resume");
            SetState(XRSessionState.Running);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (State == XRSessionState.Ready)
                return Task.CompletedTask;
            if (State != XRSessionState.Running && State != XRSessionState.Paused && State != XRSessionState.Failed)
                throw new InvalidOperationException($"Cannot stop while provider state is {State}.");
            SetState(XRSessionState.Stopping);
            SetState(XRSessionState.Ready);
            return Task.CompletedTask;
        }

        /// <summary>Emits a mock runtime error for consumer UI testing.</summary>
        public void ReportMockError() =>
            ErrorReported?.Invoke(new XRSessionError("mock.runtime", "This is a sample XR provider error."));

        private void RequireState(XRSessionState expected, string operation)
        {
            if (State != expected)
                throw new InvalidOperationException($"Cannot {operation} while provider state is {State}.");
        }

        private void SetState(XRSessionState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
