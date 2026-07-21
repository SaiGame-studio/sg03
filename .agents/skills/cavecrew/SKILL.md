---
name: cavecrew
description: >
  Decision guide for delegating to caveman-style subagents. Tells the main
  thread WHEN to spawn `cavecrew-investigator` (locate code), `cavecrew-builder`
  (1-2 file edit), or `cavecrew-reviewer` (diff review) instead of doing the
  work inline or using vanilla `Explore`. Subagent output is caveman-compressed
  so the tool-result injected back into main context is ~60% smaller — main
  context lasts longer across long sessions.
  Trigger: "delegate to subagent", "use cavecrew", "spawn investigator/builder/reviewer",
  "save context", "compressed agent output".
---
Cavecrew = 3 subagent presets emitting caveman output to shrink tool-result tokens in main-context.

## Selection Matrix
| Task | Subagent | Use case / Notes |
| :--- | :--- | :--- |
| Locate code/symbols | `cavecrew-investigator` | "Where is X", "what calls Y", "uses of Z". Read-only. |
| Suggest/architecture | `Explore` (vanilla) | Need prose/discussion. |
| Surgical edit (≤2 files) | `cavecrew-builder` | Scope obvious. Refuses 3+ files. |
| Big change / Refactor | Main thread / `feature-dev` | Complex changes. |
| Audit diff / PR | `cavecrew-reviewer` | Line-by-line bugs/warnings. Terse output. |
| Deep review | `Code Reviewer` (vanilla) | Rationale/alternatives. |
| 1-line answer | Main thread | No subagent. |

Rule: Pick cavecrew for 1/3 tokens, vanilla for prose.

## Output Formats
- **`cavecrew-investigator`**: `<Header>:\n- path:line — \`symbol\` — note\ntotals: <counts>.\n` (or `No match.`)
- **`cavecrew-builder`**: `<path:line-range> — <change ≤10 words>.\nverified: <re-read OK | mismatch @ path:line>.\n` (or: `too-big.` / `needs-confirm.` / `ambiguous.` / `regressed.`)
- **`cavecrew-reviewer`**: `path:line: <emoji> <severity>: <problem>. <fix>.\ntotals: N🔴 N🟡 N🔵 N❓\n` (or `No issues.`)

## Patterns
- **Locate → Fix → Verify**: `investigator` → `builder` → `reviewer`.
- **Parallel scout**: Spawn 2-3 `investigators` in parallel, aggregate.
- **Direct edit**: Know site → run `builder` directly.

## Boundaries
- No `builder` without knowing file.
- No `builder` for 5+ files.
- No "general feedback" from `reviewer`.
- Drop caveman for security warnings, confirmation, ambiguity.
