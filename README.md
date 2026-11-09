# Pixel Play Foundation

Reusable, platform-independent gameplay systems for Pixel Play Unity projects. The repository is structured as a Unity Package Manager (UPM) package so it can be added to projects through Git.

## Current systems

- `CountdownTimer`: explicitly advanced timer with pause, resume, reset, tick, and completion events.
- `ScoreCounter`: integer score with add, subtract, set, reset, and change event.
- `IResettable`: small shared interface for systems with a reset operation.
- `IKeyValueStore` and `PlayerPrefsKeyValueStore`: typed local preferences behind a platform-independent interface and an optional Unity adapter.
- `IHttpClient`: platform-independent HTTP request/response contract with an optional `UnityWebRequest` adapter.

The runtime assembly has no Unity engine references. A game can adapt Unity's frame loop to these systems and choose scaled or unscaled time.

## Install in a Unity project

In Unity, open **Window > Package Management > Package Manager**, choose **Add package from git URL...**, and enter the repository URL. Unity can use the repository root because it contains `package.json`.

To import the scene example, select this package in Package Manager, open **Samples**, and choose **Import** for **Foundation Quick Start**. Add `FoundationQuickStart` to a GameObject. For the visual HUD, create a Canvas with two **Text - TextMeshPro** elements, add `FoundationQuickStartHUD` to the same GameObject, and assign the timer and score fields in the Inspector. The sample requires TextMeshPro; install `com.unity.textmeshpro` if needed and import TMP Essential Resources from **Window > TextMeshPro**. In Play mode, the component context menu includes **Add Points** and **Reset Demo**. `pointsPerElapsedSecond` is awarded once for each full second as the timer runs; `fixedCompletionBonus` is added when it finishes. Set either value to zero to disable that part.

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

For a POST, pass a method, UTF-8 body, and headers to `HttpRequestData`; for example, add a `Content-Type: application/json` header. The module does not choose a JSON library or backend, and it does not log request headers or bodies.

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

Keep the core systems independent of scenes, UI, input devices, and service providers. Add optional integrations in separate assemblies or packages when a real project needs them. See [`Documentation~/architecture.md`](Documentation~/architecture.md) for the initial conventions.
