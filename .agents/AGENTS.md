# Agent Rules

- **No Vietnamese Comments in Code**: Always write comments and documentation in code files in English, even if the user interacts with you in Vietnamese. Do not use Vietnamese comments in the codebase.
- **Maximum File Length**: A single code file should never exceed 700 lines. If a file is approaching this limit, suggest refactoring or splitting it into multiple files.
- **Prioritize Event Actions**: Always prioritize using event actions over `Update()` or similar continuous loops, unless it is impossible to implement the required logic with event actions.
- **VisualElement Identification**: All `VisualElement` instances must have a `name` or `id` to uniquely identify them.
- **_sg03-Only Changes**: Agents assigned to _sg03-scoped UI work may only create, edit, move, or delete files under `Assets/_sg03`. They must not modify files elsewhere in the repository.
