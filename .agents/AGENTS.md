# Agent Rules

- **No Vietnamese Comments in Code**: Always write comments and documentation in code files in English, even if the user interacts with you in Vietnamese. Do not use Vietnamese comments in the codebase.
- **English UI Text Only**: All user-facing UI text, including labels, buttons, tooltips, empty states, and error messages, must be written in English. Do not add Vietnamese text to UI assets or code.
- **Maximum File Length**: A single code file should never exceed 700 lines. If a file is approaching this limit, suggest refactoring or splitting it into multiple files.
- **Prioritize Event Actions**: Always prioritize using event actions over `Update()` or similar continuous loops, unless it is impossible to implement the required logic with event actions.
- **VisualElement Identification**: All `VisualElement` instances must have a `name` or `id` to uniquely identify them.
- **_sg03-Only Changes**: Agents assigned to _sg03-scoped UI work may only create, edit, move, or delete files under `Assets/_sg03`. They must not modify files elsewhere in the repository.
- **Verify C# Compilation**: After writing or modifying C# code, always check and verify that the code compiles without build errors.
- **WebGL Icon Assets**: Do not use emoji or custom USS geometry elements to build UI icons. Do not manually generate custom SVG icons; always use official `.svg` vector icons downloaded directly from FontAwesome or WebGL-compatible serialized sprite/vector image assets from valid project asset sources.

