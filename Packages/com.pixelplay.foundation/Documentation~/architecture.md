# Package architecture

## Principles

1. Keep reusable logic independent of a specific game scene or platform where practical.
2. Give each system a small API and clear ownership of its state.
3. Let projects connect systems to Unity lifecycle, UI, input, and services through thin adapters.
4. Keep optional SDK integrations out of the base runtime assembly.
5. Return HTTP status errors as responses so game code can handle API-specific outcomes; reserve transport exceptions for communication failures.
6. Inject serializers and service-specific behavior instead of coupling the HTTP core to a vendor or JSON package.
7. Include a concise usage example and document edge behavior for each public system.

## Layout

- `Runtime/Core`: shared contracts and small primitives.
- `Runtime/Events`: typed event contracts and a single-thread event bus.
- `Runtime/Input`: device-independent logical input contracts and value types.
- `Runtime/Tracking`: SDK-independent tracked entity, landmark, frame, error, and provider contracts.
- `Runtime/XR`: SDK-independent VR, AR, and mixed reality session lifecycle contract.
- `Runtime/Debugging`: engine-independent runtime tunable registry and typed bounded values.
- `Runtime/Persistence`: key-value storage and serializer-independent, versioned JSON save slots.
- `Runtime/Pools`: bounded, factory-based object pool independent of Unity.
- `Runtime/QrCodes`: dependency-free UTF-8 QR matrix encoder for short payloads.
- `Runtime/Gestures`: SDK-independent hand gesture candidate, frame, provider, and stabilization filter contracts.
- `Runtime/<Feature>`: runtime systems grouped by capability.
- `Runtime/Networking`: transport-independent HTTP contracts and data types.
- `Runtime/StateMachine`: platform-independent game flow state lifecycle.
- `Runtime/Unity`: optional Unity-specific adapters in a separate assembly.
- `Runtime/Unity/UI`: EventSystem-based reusable UI drag and drop components, compiled when Unity UI is installed.
- `Runtime/Unity/Camera`: Unity webcam preview and device lifecycle adapter, compiled when Unity UI is installed.
- `Runtime/Unity/Tracking`: Unity screen-space debug visualization for tracking frames.
- `Runtime/Unity/Debugging`: per-project Inspector tuning definitions and the development diagnostics panel.
- `Runtime/Unity/Gestures`: Unity lifecycle bridge between gesture providers and the core filter.
- `Runtime/Unity/QrCodes`: optional Unity UI texture renderer for QR matrices.
- `Runtime/Unity/Persistence`: PlayerPrefs adapter, Unity JSON serializer, and persistentDataPath save-slot wrapper.
- `Runtime/Unity/Pools`: GameObject prefab pooling adapter for gameplay objects.
- `Editor`: future editor-only tooling, in its own assembly.
- `Samples~`: optional sample content, imported through Package Manager.
- `Documentation~`: package documentation, excluded from consuming builds.

## Naming

- Package ID: `com.pixelplay.foundation`.
- C# namespace root: `PixelPlay.Foundation`.
- Public types use descriptive names and XML summaries.
- Avoid a dependency from core systems to UI, scenes, input, or vendor SDKs.

## Compatibility

The initial package targets Unity 2021.3 LTS and uses a runtime assembly definition. Revisit the minimum Unity version if a consuming project requires an older editor or a newer package API.
