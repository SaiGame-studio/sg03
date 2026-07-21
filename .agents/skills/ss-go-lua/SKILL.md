---
name: ss-go-lua
description: Generate, review, debug, validate, or explain Lua 5.1 scripts for the ss-go game server runtime. Use for ss-go or SaiGame Lua tasks involving payload, ctx, output, game.* APIs, library scripts, inventory, gacha, entities, quests, presets, or battle sessions.
---

# ss-go Lua

## Workflow

1. Read `references/CONTRACT.md` completely before producing or reviewing Lua code. Treat it as the canonical runtime contract.
2. Identify the requested game logic, expected `payload` schema, and expected `output` schema. Read `references/RETURN_SCHEMAS.md` before accessing fields returned by `game.*`. Do not invent missing fields, globals, helpers, return shapes, or `game.*` APIs.
3. Produce simple Lua 5.1 code that stays within the documented sandbox and runtime limits.
4. Check every `err` returned by `game.*` before using returned data. Stop early with `output.error` when validation or a game API call fails.
5. Write response data to `output`. Return only the Lua script body without Markdown fences unless the user explicitly asks for an explanation.

## Library Scripts

For regular scripts, use only the documented top-of-file server preprocessor form `require "libname"`. Do not call the disabled Lua `require(...)` builtin.

For library scripts, define functions only. Do not add top-level executable statements or nested library directives.

## Bundled Resources

- Read `references/AGENT_PROMPT.md` only when preparing a standalone prompt for another AI tool or verifying that the distributed prompt remains consistent with the canonical contract.
- Use `references/RETURN_SCHEMAS.md` as the canonical field-level contract for values returned by `game.*`.
- Use `assets/ss-go-game-api.lua` as an editor contract stub. Do not execute it as production Lua code.
- Use `assets/luarc.snippet.json` only as a configuration snippet to merge into an existing Lua Language Server configuration. Never overwrite an existing project `.luarc.json` without explicit user approval.
