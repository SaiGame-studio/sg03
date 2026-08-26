# Repository Rules

## SaiGame Package Boundary

Never create, modify, move, rename, or delete files under `Assets/SaiGame/`, except files directly inside `Assets/SaiGame/LuaScript/Scripts/`.

Treat `Assets/SaiGame/` as a read-only dependency. Implement project-specific behavior only outside that directory, except for game-specific Lua scripts directly inside `Assets/SaiGame/LuaScript/Scripts/`, unless the user explicitly revokes this rule for a specific change.

## Game Content Naming

When creating or editing game content, use English for every official character and skill name. Vietnamese may be used only in descriptive text or explanatory notes, never as an official name, identifier, or card title.

## Unity Lifecycle Methods

Do not place implementation logic directly in Unity lifecycle methods (for example `Awake`, `Start`, `OnEnable`, `Update`, `LateUpdate`, `OnDisable`, or `OnDestroy`). Lifecycle methods may only call clearly named helper methods; put all logic in those helper methods instead.

## Unity Component Loading

Resolve Unity component references through the `LoadComponents()` mechanism. Every `SaiBehaviour` that needs component dependencies must override `LoadComponents()`, call `base.LoadComponents()` first, and invoke clearly named `Load...` helper methods for those dependencies.

Stable scene and prefab dependencies should also have serialized references. A `Load...` helper may use `GetComponent`, `FindFirstObjectByType`, or a similar lookup only as a fallback when its cached or serialized reference is null.

Do not resolve missing component references lazily from gameplay actions, event callbacks, request callbacks, UI handlers, or other runtime execution paths. Those paths must consume references already prepared by `LoadComponents()` and fail clearly if a required reference is unavailable.

## Runtime UI Assets

For any UI Toolkit asset required at runtime (`VisualTreeAsset`, `StyleSheet`, `PanelSettings`, and related assets), assign a serialized reference in the owning scene or prefab. That reference must be present in source control so Unity includes the asset in every player build, including WebGL.

`UnityEditor.AssetDatabase` is editor-only and must never be the only way a runtime UI asset is loaded. It may be used solely inside `#if UNITY_EDITOR` as a convenience fallback; the runtime serialized reference remains mandatory.

Before completing a UI change, inspect every affected scene/prefab serialization and verify that each newly used runtime asset has a non-null reference. Do not rely on an asset loading successfully in the Unity Editor as evidence it will work in a player build.

### WebGL Icon Compatibility

Do not use emoji or non-ASCII Unicode characters as UI icons. UI Toolkit's player fonts, especially in WebGL, may not include their glyphs even when they display in the Unity Editor. Use serialized sprite/vector assets or UI Toolkit geometry styled in USS; these assets must follow the runtime-reference rule above. Plain ASCII text is acceptable for a textual fallback.
