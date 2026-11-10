using System.Collections.Generic;
using PixelPlay.Foundation.Debugging;
using UnityEngine;
using UnityEngine.Profiling;

namespace PixelPlay.Foundation.Unity.Debugging
{
    /// <summary>Development-only IMGUI panel for project tunables and runtime diagnostics.</summary>
    public sealed class RuntimeDebugPanel : MonoBehaviour
    {
        [SerializeField] private RuntimeTuningRegistryBehaviour tuningRegistry;
        [SerializeField] private bool showOnEnable = true;
        [SerializeField] private bool developmentBuildOnly = true;
        [SerializeField] private bool captureInfoLogs;
        [SerializeField, Min(1)] private int maximumLogEntries = 40;
        [SerializeField, Min(1)] private int visibleLogEntries = 6;

        private readonly object _logLock = new object();
        private readonly List<string> _logs = new List<string>();
        private Vector2 _scrollPosition;
        private bool _visible;
        private float _framesPerSecond;
        private float _frameTimeMilliseconds;
        private GUIStyle _headingStyle;

        private void OnEnable()
        {
            _visible = showOnEnable;
            Application.logMessageReceivedThreaded += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceivedThreaded -= HandleLog;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
                return;

            float frameRate = 1f / deltaTime;
            _framesPerSecond = Mathf.Lerp(_framesPerSecond, frameRate, 0.1f);
            _frameTimeMilliseconds = Mathf.Lerp(_frameTimeMilliseconds, deltaTime * 1000f, 0.1f);
        }

        /// <summary>Shows the panel; can be wired to a project-specific debug button.</summary>
        public void ShowPanel() => _visible = true;

        /// <summary>Hides the panel.</summary>
        public void HidePanel() => _visible = false;

        /// <summary>Toggles visibility; no particular input backend is required.</summary>
        public void TogglePanel() => _visible = !_visible;

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Log && !captureInfoLogs)
                return;

            string entry = $"[{type}] {condition}";
            lock (_logLock)
            {
                _logs.Add(entry);
                int maximum = maximumLogEntries > 0 ? maximumLogEntries : 1;
                if (_logs.Count > maximum)
                    _logs.RemoveRange(0, _logs.Count - maximum);
            }
        }

        private void OnGUI()
        {
            if (developmentBuildOnly && !Application.isEditor && !Debug.isDebugBuild)
                return;

            if (!_visible)
            {
                if (GUI.Button(new Rect(12f, 12f, 104f, 28f), "Show Debug"))
                    _visible = true;
                return;
            }

            float width = Mathf.Min(470f, Mathf.Max(200f, Screen.width - 24f));
            float height = Mathf.Min(740f, Mathf.Max(180f, Screen.height - 24f));
            EnsureHeadingStyle();
            GUILayout.BeginArea(new Rect(12f, 12f, width, height), "Pixel Play Debug", GUI.skin.window);
            GUILayout.BeginHorizontal();
            DrawRuntimeSummary();
            if (GUILayout.Button("Hide", GUILayout.Width(54f)))
                _visible = false;
            GUILayout.EndHorizontal();

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            DrawTunables();
            DrawLogs();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawRuntimeSummary()
        {
            GUILayout.BeginVertical();
            GUILayout.Label($"{_framesPerSecond:0} FPS  |  {_frameTimeMilliseconds:0.0} ms");
            GUILayout.Label($"{Application.platform}  |  {Screen.width} x {Screen.height}  |  {Application.version}");
            GUILayout.Label($"Memory: {Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f):0.0} MB");
            GUILayout.EndVertical();
        }

        private void DrawTunables()
        {
            RuntimeTuningRegistry registry = tuningRegistry != null ? tuningRegistry.Registry : null;
            if (registry == null || registry.Settings.Count == 0)
            {
                GUILayout.Label("No runtime tuning values are registered.");
                return;
            }

            EnsureHeadingStyle();
            GUILayout.Space(8f);
            GUILayout.Label("Runtime Values", _headingStyle);
            string currentCategory = null;

            for (int i = 0; i < registry.Settings.Count; i++)
            {
                RuntimeTunable tunable = registry.Settings[i];
                if (currentCategory != tunable.Category)
                {
                    currentCategory = tunable.Category;
                    GUILayout.Label(currentCategory, _headingStyle);
                }

                DrawTunable(tunable);
            }

            if (GUILayout.Button("Reset Defaults"))
                registry.ResetAll();
        }

        private void DrawTunable(RuntimeTunable tunable)
        {
            GUIContent label = new GUIContent(tunable.DisplayName, tunable.Description);
            if (tunable is FloatTunable floatValue)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(150f));
                float next = GUILayout.HorizontalSlider(floatValue.Value, floatValue.Minimum, floatValue.Maximum);
                GUILayout.Label(next.ToString("0.##"), GUILayout.Width(54f));
                GUILayout.EndHorizontal();
                floatValue.SetValue(next);
            }
            else if (tunable is IntegerTunable integerValue)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(label, GUILayout.Width(150f));
                float slider = GUILayout.HorizontalSlider(integerValue.Value, integerValue.Minimum, integerValue.Maximum);
                int next = Mathf.Clamp(Mathf.RoundToInt(slider), integerValue.Minimum, integerValue.Maximum);
                GUILayout.Label(next.ToString(), GUILayout.Width(54f));
                GUILayout.EndHorizontal();
                integerValue.SetValue(next);
            }
            else if (tunable is BooleanTunable booleanValue)
            {
                booleanValue.SetValue(GUILayout.Toggle(booleanValue.Value, label));
            }
        }

        private void DrawLogs()
        {
            List<string> recentLogs;
            lock (_logLock)
                recentLogs = new List<string>(_logs);

            GUILayout.Space(10f);
            GUILayout.Label($"Recent Logs ({recentLogs.Count})", _headingStyle);
            int first = Mathf.Max(0, recentLogs.Count - Mathf.Max(1, visibleLogEntries));
            for (int i = first; i < recentLogs.Count; i++)
                GUILayout.Label(recentLogs[i], GUI.skin.box);
        }

        private void EnsureHeadingStyle()
        {
            if (_headingStyle != null)
                return;

            _headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 15
            };
        }
    }
}
