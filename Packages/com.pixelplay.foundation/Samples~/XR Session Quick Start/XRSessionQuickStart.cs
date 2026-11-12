using System;
using PixelPlay.Foundation.XR;
using UnityEngine;

namespace PixelPlay.Foundation.Samples
{
    /// <summary>Demonstrates calling an XR session provider through its SDK-independent contract.</summary>
    public sealed class XRSessionQuickStart : MonoBehaviour
    {
        private MockXRSessionProvider _provider;

        private MockXRSessionProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    _provider = new MockXRSessionProvider();
                    _provider.StateChanged += HandleStateChanged;
                    _provider.ErrorReported += HandleError;
                }
                return _provider;
            }
        }

        [ContextMenu("Start Mock XR Session")]
        private async void StartMockSession() => await Run(Provider.StartAsync);

        [ContextMenu("Pause Mock XR Session")]
        private async void PauseMockSession() => await Run(Provider.PauseAsync);

        [ContextMenu("Resume Mock XR Session")]
        private async void ResumeMockSession() => await Run(Provider.ResumeAsync);

        [ContextMenu("Stop Mock XR Session")]
        private async void StopMockSession() => await Run(Provider.StopAsync);

        [ContextMenu("Report Mock XR Error")]
        private void ReportMockError() => Provider.ReportMockError();

        private async System.Threading.Tasks.Task Run(Func<System.Threading.CancellationToken, System.Threading.Tasks.Task> operation)
        {
            try
            {
                await operation(default);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(exception.Message, this);
            }
        }

        private void HandleStateChanged(XRSessionState state) => Debug.Log($"XR session: {state} ({Provider.Mode}).", this);
        private void HandleError(XRSessionError error) => Debug.LogError($"XR error [{error.Code}]: {error.Message}", this);

        private void OnDestroy()
        {
            if (_provider == null)
                return;
            _provider.StateChanged -= HandleStateChanged;
            _provider.ErrorReported -= HandleError;
        }
    }
}
