# Repository Rules

## SaiGame Package Boundary

Never create, modify, move, rename, or delete files under `Assets/SaiGame/`, except files directly inside `Assets/SaiGame/LuaScript/Scripts/`.

Treat `Assets/SaiGame/` as a read-only dependency. Implement project-specific behavior only outside that directory, except for game-specific Lua scripts directly inside `Assets/SaiGame/LuaScript/Scripts/`, unless the user explicitly revokes this rule for a specific change.
