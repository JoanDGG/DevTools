# Pixel Play Foundation

Reusable, platform-independent gameplay systems for Pixel Play Unity projects. The repository is structured as a Unity Package Manager (UPM) package so it can be added to projects through Git.

## Current systems

- `CountdownTimer`: explicitly advanced timer with pause, resume, reset, tick, and completion events.
- `ScoreCounter`: integer score with add, subtract, set, reset, and change event.
- `IResettable`: small shared interface for systems with a reset operation.
- `IEventBus` and `EventBus`: type-safe event publication with disposable subscriptions.
- `IKeyValueStore` and `PlayerPrefsKeyValueStore`: typed local preferences behind a platform-independent interface and an optional Unity adapter.
- `GameStateMachine`: one-at-a-time state lifecycle manager with explicit entry, exit, and tick calls.
- `IInputActionReader`: device-independent action queries with portable scalar, vector, and rotation value types; Unity Input System support is an optional adapter.
- `RuntimeTuningRegistry`: typed, bounded per-project values with change notifications and reset-to-default support.
- `IHttpClient`: platform-independent HTTP request/response contract with an optional `UnityWebRequest` adapter.
- `JsonHttpClient`: composes the HTTP transport with an injectable JSON serializer; Unity also has a `JsonUtility` adapter.
- `MultipartFormDataBuilder`: creates multipart requests with text fields and binary file parts.
- `BearerTokenHttpClient`: decorates an HTTP transport with a token from an injectable access-token provider.

The runtime assembly has no Unity engine references. A game can adapt Unity's frame loop to these systems and choose scaled or unscaled time.

## Runtime debug panel and tuning values

Use `RuntimeTuningRegistry` to define editable float, integer, and boolean values with categories, defaults, ranges, and change events. A project can register values in code or define them on `RuntimeTuningRegistryBehaviour` in the Inspector. Game code reads these same values, so adjusting them in the panel changes what the game uses immediately. `RuntimeDebugPanel` displays FPS, frame time, platform, screen size, allocated memory, and recent warnings/errors. It is shown in the Editor and development builds by default, and its visibility methods can be wired to a project's own input actions or UI. Values reset to their defaults on application restart; persistence is left to the project.

Import **Runtime Debug Quick Start** from Package Manager's **Samples** tab. Add `RuntimeTuningRegistryBehaviour`, `RuntimeDebugPanel`, and `RuntimeDebugQuickStartConsumer` to a GameObject, then assign the registry to the other components. The consumer adds defaults for `round.duration` and `round.count` if those IDs are not already configured. To customize them in the Inspector, add definitions with those IDs, float/integer types, and ranges; the sample logs changes and the **Log Current Game Settings** context action prints the current values. Set `developmentBuildOnly` off only when you intentionally want the panel in a non-development player.

```csharp
RuntimeTuningRegistry registry = tuningRegistry.Registry;
FloatTunable duration = registry.GetFloat("round.duration");
IntegerTunable rounds = registry.GetInteger("round.count");

duration.ValueChanged += changed => Debug.Log($"Round duration: {duration.Value}");
float secondsPerRound = duration.Value;
int totalRounds = rounds.Value;
```

## Tracking providers

Use `ITrackingProvider` to keep body or hand tracking consumers independent of the SDK. Providers emit immutable `TrackingFrame` snapshots containing identified people and named landmarks. X and Y coordinates are normalized to the source image with a top-left origin; depth uses provider-defined units, and confidence is normalized from zero to one. The contract does not prescribe a landmark schema, model, or hardware.

Import **Tracking Provider Quick Start** from Package Manager's **Samples** tab. Add `MockBodyTrackingProvider` and `TrackingFrameDebugOverlay` to a GameObject, then assign the provider to the overlay. The mock emits an animated skeleton so you can test consumers and normalized coordinate visualization before choosing a tracking SDK. Providers should raise callbacks on Unity's main thread when their listeners use Unity objects; the debug overlay safely snapshots callback data for drawing.

`PixelPlay.Foundation.Unity.Tracking` contains the screen-space debug overlay. A production adapter can implement `ITrackingProvider`, translate its SDK's results to `TrackingFrame`, and expose provider-specific camera permissions and model configuration separately.

## Hand gesture recognition

`IHandGestureProvider` emits one top gesture candidate per hand per frame, with a stable hand ID, provider-defined gesture ID, and confidence from zero to one. Frame timestamps should use a monotonic seconds clock. `GestureRecognitionFilter` applies separate start and release confidence thresholds plus activation and release durations. The hysteresis and timing filters prevent short confidence dips from rapidly starting and ending gestures. IDs are intentionally strings so each SDK can map its own gesture names without changing the package.

Import **Gesture Recognition Quick Start** from Package Manager's **Samples** tab. Add `MockHandGestureProvider`, `GestureRecognitionFilterBehaviour`, and `GestureRecognitionQuickStartConsumer` to one GameObject and assign the mock provider to the filter. The mock alternates open palm and pinch for its left hand, reports thumbs up for its right hand, and adds brief confidence dips; the Console shows only filtered start/end events.

A real SDK adapter should implement `IHandGestureProvider` and publish its strongest candidate for each visible hand. The Unity bridge can optionally start the provider when enabled and stop it when disabled; leave those options off when another system owns the provider lifecycle. Tune confidence and duration thresholds per provider; this module filters classifications but does not analyze camera pixels or infer gestures from landmarks.

## Install in a Unity project

This package is embedded in the DevTools Unity project at `Packages/com.pixelplay.foundation`. To install it in another project, open Package Manager, choose **Add package from git URL...**, and use `https://github.com/JoanDGG/DevTools.git?path=/Packages/com.pixelplay.foundation`.

To import the timer and score sample, select this package in Package Manager, open **Samples**, and choose **Import** for **Foundation Quick Start**. Add `FoundationQuickStart` to a GameObject. For its HUD, create a Canvas with two **Text - TextMeshPro** elements, add `FoundationQuickStartHUD` to the same GameObject, and assign the timer and score fields in the Inspector. The sample requires TextMeshPro; install `com.unity.textmeshpro` if needed and import TMP Essential Resources from **Window > TextMeshPro**. In Play mode, the component context menu includes **Add Points** and **Reset Demo**. `pointsPerElapsedSecond` is awarded once for each full second as the timer runs; `fixedCompletionBonus` is added when it finishes. Set either value to zero to disable that part.

To import **Game Flow Quick Start**, select it under the package's **Samples** tab and choose **Import**. Create a Canvas with one TextMeshPro label and five UI Buttons. Add `GameFlowQuickStart` and `GameFlowQuickStartHUD` to a controller GameObject, then assign the label and buttons in the Inspector. Wire the button labels as Start/Restart, Pause, Resume, Finish, and Return to Menu. The flow controller publishes typed state and status events; the HUD subscribes through the event bus, updates the controls and disposes its subscriptions when disabled.

The sample pause state only demonstrates a flow transition; connect its `Enter` and `Exit` behavior to your game's pause systems when you use this pattern in a project.

## Input actions

Use logical action names such as `Move`, `Confirm`, or `Grip` in gameplay code. `IInputActionReader` keeps that code independent of the keyboard, touch, gamepad, or XR bindings configured in the action asset. The optional `UnityInputActionReader` adapter is compiled when `com.unity.inputsystem` is installed. It owns the enabled state of its action map, so give it a map whose lifetime it can control.

Import **Input System Quick Start** from Package Manager's **Samples** tab. Assign an `InputActionAsset`, action map name, a Vector2 action for movement, and a button action for confirmation. The sample moves its target using the logical movement action and logs confirmation presses. Add bindings for the devices your project supports to those same actions.

```csharp
using PixelPlay.Foundation.Input;
using PixelPlay.Foundation.Unity.InputSystem;

var input = new UnityInputActionReader(inputActions, "Player");
input.Enable();

InputVector2 move = input.ReadVector2(new InputActionId("Move"));
bool confirmPressed = input.WasPressedThisFrame(new InputActionId("Submit"));

input.Dispose(); // Disables the action map and releases cached references.
```

## UI drag and drop

`UIDraggableItem` and `UIDropTarget` use Unity's EventSystem pointer interfaces, so the project can drive them with its configured mouse, touch, or other pointer input module. Add `UIDraggableItem` to a UI element with a raycastable `Graphic`; its required `CanvasGroup` temporarily allows drop targets underneath it to receive raycasts. Add `UIDropTarget` to a raycastable UI element. Leave the target type empty to accept any item, or set matching item and accepted types to filter drops. Rejected drops return to the original parent and position. Accepted drops can become children of the target, and both components expose Inspector events.

Import **UI Drag and Drop Quick Start** from Package Manager's **Samples** tab. Add an EventSystem and a Canvas with a Graphic Raycaster, then use raycastable UI Images as a target and a draggable item. Set the item type and matching target type (for example, `gem`), add the HUD script to a controller, and assign the target and a TextMeshPro UI label. The target's **On Item Dropped** event updates the sample label. Install `com.unity.textmeshpro` and import TMP Essential Resources if needed.

The components live in the optional `PixelPlay.Foundation.Unity.UI` assembly, which references Unity UI (`Unity.ugui`). They don't depend on the Input System package; the project's EventSystem input module handles pointer device support.

## Webcam preview

`WebcamPreview` requests camera authorization, selects a device by exact name or optionally prefers a front-facing camera, starts a `WebCamTexture`, and displays it in a UI `RawImage`. It waits for the first frame before reporting success, exposes the active texture and selected device name, and stops/releases the stream when disabled. `WebcamSnapshotCapture` can return PNG bytes for upload or save a uniquely named PNG under `Application.persistentDataPath`. `MultipartImageUploader` sends PNG bytes as a configurable multipart `POST`, supports extra text fields, an optional `IAccessTokenProvider`, cancellation, and returns HTTP status and body for API-specific handling.

Import **Webcam Preview Quick Start** from Package Manager's **Samples** tab. Add a Canvas with a `RawImage`, a TextMeshPro label, and Buttons for capture/save and upload. Add `WebcamPreview`, `WebcamSnapshotCapture`, `MultipartImageUploader`, and `WebcamPreviewQuickStartHUD` to a GameObject and assign their fields. Connect one Button's **On Click** to `WebcamSnapshotCapture.CaptureAndSave` and the other to `WebcamPreviewQuickStartHUD.CaptureAndUpload`. Set the uploader endpoint, image field name, and any additional form fields to match your API. The HUD displays the save path or HTTP result. Leave the preferred device name blank to use the first available camera, or enable front-facing preference. The camera permission prompt is shown at runtime where the platform requires it.

The camera components live in the `PixelPlay.Foundation.Unity.Camera` assembly and compile when `com.unity.ugui` is installed. The uploader uses `PixelPlay.Foundation.Unity` and the existing multipart/networking contracts. If a token provider is assigned, authenticated non-loopback URLs must use HTTPS. On iOS and WebGL, the preview requests camera authorization before enumerating devices; WebGL camera access requires a secure browser context (HTTPS). On Android, Unity adds and requests camera permission when `WebCamTexture` is first used. Add an iOS camera usage description in Player Settings. Camera availability and resolution remain platform-specific; the preview reports denial or startup timeout through `On Error`. Captured PNG bytes reflect the raw camera frame orientation; apply orientation or mirroring if the consuming flow needs to match a transformed preview.

## Event bus

Create one bus per game or feature scope and pass it to publishers and listeners that need to communicate without direct references. Subscriptions return `IDisposable`; keep the token and dispose it when the listener is disabled or destroyed. The implementation is single-threaded and intended for Unity's main thread.

```csharp
using System;
using PixelPlay.Foundation.Events;

public readonly struct ScoreChanged
{
    public ScoreChanged(int previous, int current)
    {
        Previous = previous;
        Current = current;
    }

    public int Previous { get; }
    public int Current { get; }
}

IEventBus events = new EventBus();
IDisposable scoreSubscription = events.Subscribe<ScoreChanged>(change =>
    UnityEngine.Debug.Log($"Score: {change.Previous} -> {change.Current}"));

events.Publish(new ScoreChanged(0, 100));
scoreSubscription.Dispose();
```

Publishing uses a snapshot of current listeners. Subscriptions added during a publish receive later events; listeners disposed before their turn are skipped. Listener exceptions propagate to the publisher.

## Local preferences

Use the core `IKeyValueStore` contract in game logic and inject a `PlayerPrefsKeyValueStore` in Unity projects. The adapter accepts an optional key prefix so multiple modules can keep their keys separate. Call `Save()` when you need to explicitly flush changes. PlayerPrefs is for local preferences and progress, not secrets or tamper-proof data.

```csharp
using PixelPlay.Foundation.Unity.Persistence;

var settings = new PlayerPrefsKeyValueStore("PixelPlay.MyGame.");
settings.SetInt("high-score", 1200);
settings.Save();
int highScore = settings.GetInt("high-score", 0);
```

## HTTP requests

Game logic can depend on `IHttpClient`; Unity projects can provide `UnityWebRequestHttpClient`. HTTP error status codes are returned in `HttpResponseData` for the caller to handle. Connection and data-processing failures throw `HttpTransportException`, and cancellation throws `OperationCanceledException`.

```csharp
using PixelPlay.Foundation.Networking;
using PixelPlay.Foundation.Unity.Networking;

IHttpClient http = new UnityWebRequestHttpClient();
HttpResponseData response = await http.SendAsync(
    new HttpRequestData("https://example.com/api/health", timeoutSeconds: 10),
    cancellationToken);

if (response.IsSuccessStatusCode)
    UnityEngine.Debug.Log(response.BodyText);
else
    UnityEngine.Debug.LogWarning($"Server returned HTTP {response.StatusCode}");
```

For JSON requests, compose `JsonHttpClient` with a serializer. The Unity adapter uses serializable DTO classes with public fields; projects that need broader JSON support can provide another `IJsonSerializer` implementation.

```csharp
using System;
using System.Threading;
using PixelPlay.Foundation.Networking;
using PixelPlay.Foundation.Unity.Networking;

[Serializable]
public class ScoreUpload
{
    public string playerId;
    public int score;
}

[Serializable]
public class ScoreReply
{
    public string message;
}

var json = new JsonHttpClient(new UnityWebRequestHttpClient(), new UnityJsonUtilitySerializer());
JsonHttpResponse<ScoreReply> result = await json.SendJsonAsync<ScoreUpload, ScoreReply>(
    "https://example.com/api/scores", "POST",
    new ScoreUpload { playerId = "player-1", score = 1200 },
    cancellationToken: CancellationToken.None);

if (result.IsSuccessStatusCode)
    UnityEngine.Debug.Log(result.Payload.message);
```

`JsonUtility` has serialization limits (for example, it is not a general-purpose dictionary serializer). The module does not log request headers or bodies.

## Multipart uploads

Use `MultipartFormDataBuilder` to send an image or other binary file alongside text fields. It creates the boundary and matching content type, so add authentication and other custom headers through `BuildRequest` but do not set `Content-Type` yourself.

```csharp
using System.Collections.Generic;
using System.Threading;
using PixelPlay.Foundation.Networking;
using PixelPlay.Foundation.Unity.Networking;

byte[] imageBytes = encodedImageBytes;
string accessToken = currentAccessToken;

var form = new MultipartFormDataBuilder()
    .AddField("playerId", "player-1")
    .AddFile("image", "capture.png", "image/png", imageBytes);

HttpRequestData request = form.BuildRequest(
    "https://example.com/api/images", headers: new Dictionary<string, string>
    {
        { "Authorization", "Bearer " + accessToken }
    });

HttpResponseData response = await new UnityWebRequestHttpClient().SendAsync(request, CancellationToken.None);
```

The builder buffers the full request body in memory. It is intended for images and other small or medium uploads; large files should use a streaming upload adapter.
File names are stripped of local path components and percent-encoded in the multipart header.

## Bearer authentication

Wrap any `IHttpClient` with `BearerTokenHttpClient`. The provider supplies a current token and can own whatever sign-in or refresh flow the project uses. The decorator replaces any existing `Authorization` header with `Bearer <token>`; it does not implement a login provider or retry unauthorized responses.

```csharp
using System.Threading;
using System.Threading.Tasks;
using PixelPlay.Foundation.Networking;
using PixelPlay.Foundation.Unity.Networking;

public sealed class SessionTokenProvider : IAccessTokenProvider
{
    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Session.Current.AccessToken);
    }
}

IHttpClient http = new BearerTokenHttpClient(
    new UnityWebRequestHttpClient(), new SessionTokenProvider());
HttpResponseData response = await http.SendAsync(
    new HttpRequestData("https://example.com/api/profile"), CancellationToken.None);
```

Use HTTPS for authenticated requests, and keep tokens in the session/authentication layer rather than ordinary preferences.

## Game state machine

Implement `IGameState` for menu, gameplay, pause, and results states. A Unity `MonoBehaviour` owns the machine, calls `ChangeState` to transition, and forwards `Update` time to `Tick`.

```csharp
using PixelPlay.Foundation.StateMachine;
using UnityEngine;

public class GameFlow : MonoBehaviour
{
    private GameStateMachine _states;

    private void Awake()
    {
        _states = new GameStateMachine();
        _states.StateChanged += (previous, current) =>
            Debug.Log($"State: {previous?.GetType().Name ?? "None"} -> {current?.GetType().Name ?? "None"}");
        _states.ChangeState(new MainMenuState());
    }

    private void Update() => _states.Tick(Time.deltaTime);

    private sealed class MainMenuState : IGameState
    {
        public void Enter() { }
        public void Exit() { }
        public void Tick(float deltaTime) { }
    }
}
```

The machine does not create or own state instances. It calls `Exit` on the previous state, `Enter` on the next state, and `Tick` once per owner update. A state must request transitions outside its own `Enter` or `Exit` method.

## Quick start

```csharp
using PixelPlay.Foundation.Scoring;
using PixelPlay.Foundation.Timing;
using UnityEngine;

public class RoundExample : MonoBehaviour
{
    private CountdownTimer _timer;
    private ScoreCounter _score;

    private void Start()
    {
        _timer = new CountdownTimer(30f);
        _timer.Completed += OnTimerCompleted;
        _timer.Start();

        _score = new ScoreCounter();
        _score.Changed += (previous, current) => Debug.Log($"Score: {previous} -> {current}");
        _score.Add(100);
    }

    private void Update()
    {
        _timer.Advance(Time.deltaTime); // Use Time.unscaledDeltaTime for UI/pause independent time.
    }

    private void OnTimerCompleted() => Debug.Log("Round over");
}
```

## Design direction

Keep the core systems independent of scenes, UI, input devices, and service providers. Add optional integrations in separate assemblies or packages when a real project needs them. See [`Documentation~/Architecture.md`](Documentation~/Architecture.md) for the initial conventions.
