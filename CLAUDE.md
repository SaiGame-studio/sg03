# Claude Workspace Rules

## Edit Scope
- Only create, modify, rename, or delete files inside `Assets/_sg03/`.
- Treat every path outside `Assets/_sg03/` as read-only unless the user explicitly approves a specific exception.
- Any new file must be created under `Assets/_sg03/` only.

## Operating Rules
- Reading files anywhere in the repository is allowed for context.
- Confirm the destination path is inside `Assets/_sg03/` before making changes.
- If a task requires writing outside `Assets/_sg03/`, do not proceed until the user approves that exact path.