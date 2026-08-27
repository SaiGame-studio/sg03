---
name: sg03-game-docs
description: Write or review SG03 Markdown game docs under Assets/_sg03/docs, including cards, lore, PvE, indexes, and stats. Do not use for Lua or Unity runtime implementation.
---

# SG03 Game Docs

Match the repository's taxonomy, balance rules, and nearby Markdown style.

## Context

Read `AGENTS.md`, the nearest index/overview, and one analogous doc. Load only relevant sources:

- Characters: `03_characters.md`; abilities: `04_abilities.md`.
- Stats: `05_card_star_scaling.md`.
- Lore/races: `00_world_and_races.md`.
- PvE: the nearest index/overview under `pve/`.

Paths above are relative to `Assets/_sg03/docs/`. Existing docs are authoritative. Do not invent unstated mechanics, relationships, classifications, identifiers, or stats; ask only when a missing choice changes game design.

## Rules

- All official character/skill/card names, titles, code names, and identifiers must be English—never Vietnamese. Vietnamese is only for prose.
- Never mention, explain, compare, or evaluate ATK/DEF in `Mô Tả`; keep them only in metadata.
- Use lowercase `snake_case` filenames/code names unless that content family differs.
- Put shared cards in `common/`; character-specific cards under that character.
- Copy the nearest doc's fields, ordering, headings, and link style.
- Derive stats from `05_card_star_scaling.md`; apply requested minima/maxima literally within the star tier.
- Keep scope narrow, links relative, and text UTF-8.

## Integration

For added/moved/renamed docs, update affected indexes and links. Under `Assets/`, add/preserve Unity `.meta` files; new folders need folder metadata with a unique 32-character lowercase hex GUID. Preserve GUIDs on moves. Never modify `Assets/SaiGame/`.

Review the diff for these rules, validate links/metadata, and run `git diff --check`.
