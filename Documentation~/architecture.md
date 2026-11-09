# Package architecture

## Principles

1. Keep reusable logic independent of a specific game scene or platform where practical.
2. Give each system a small API and clear ownership of its state.
3. Let projects connect systems to Unity lifecycle, UI, input, and services through thin adapters.
4. Keep optional SDK integrations out of the base runtime assembly.
5. Include a concise usage example and document edge behavior for each public system.

## Layout

- `Runtime/Core`: shared contracts and small primitives.
- `Runtime/<Feature>`: runtime systems grouped by capability.
- `Runtime/Unity`: optional Unity-specific adapters in a separate assembly.
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
