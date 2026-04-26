# Project Guidelines

## Workspace Boundaries
- Only create, modify, rename, or delete files inside `Assets/_sg03/`.
- Treat every path outside `Assets/_sg03/` as read-only unless the user gives explicit permission for a specific path.
- New files must be added under `Assets/_sg03/` only.

## Working Rules
- You may inspect any file in the repository for context.
- Before editing, verify the target path is under `Assets/_sg03/`.
- If a requested change would require writing outside `Assets/_sg03/`, stop and ask for approval instead of making the edit.

## One File — One Type
- Every `class`, `struct`, `enum`, and `interface` must live in its own dedicated file.
- The file name must exactly match the type name (e.g. `QuestType.cs` for `enum QuestType`).
- Do **not** nest enums, structs, or helper classes inside another class file.
- Do **not** define multiple top-level types in the same file.
- Do **not** create sub-classes (nested classes) inside a parent class unless the nested type is a private implementation detail that is never referenced outside that single file — and even then, prefer extracting it.

## Unity Inspector Buttons
- **Never** use `[ContextMenu]` to expose buttons.
- Always create Inspector buttons via a dedicated `[CustomEditor]` script.