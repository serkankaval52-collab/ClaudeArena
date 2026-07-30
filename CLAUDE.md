# CLAUDE.md - Premium Game Factory Conventions

## Unity MCP Core Execution Rules (DO NOT BYPASS)
*   **Sprite Serialization Rule:** When binding icons or sprites using the `manage_components` tool, never write to the `.sprite` C# property. You must write directly to the serialized backing field: `m_Sprite`.
*   **ID Lifetime Limitation:** Negatively signed GameObject IDs generated via MCP tools become invalid immediately after a prefab is compiled. Always target active scene assets using the `by_name` parameter rather than numeric ID targets.
*   **Play Mode Focus Warning:** The `enter_play_mode` command freezes indefinitely if the Unity Editor OS window is out of focus. Inform the user in Turkish to click the Unity Editor window before invoking play mode testing.
*   **Prefab Assembly Priority:** Build complex layouts, Canvas structures, and particle systems as re-usable, static `.prefab` disk assets first. Do not attempt to construct deep hierarchies component-by-component live via MCP commands.

## Code Standards
*   Maintain 100% loose coupling using `EventBus.Subscribe<T>()` and `EventBus.Publish<T>()`.
*   No GC allocations inside loops. All components must be cached inside `Awake()`.
*   Every game template MUST implement its meta-progression currencies, progressive level counts (>30), and localized main menu GDPR canvas.
*   Target Unity 2022.3 LTS with URP 14. This project is not Unity 6 / URP 17 compatible; do not use Unity 6-only APIs.
*   TextMeshPro is pinned at 3.0.6. `enableWordWrapping` is the correct API in this version; `textWrappingMode` does not exist here and will not compile.
