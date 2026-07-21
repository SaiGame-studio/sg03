# cavecrew

Tells the main thread when to spawn caveman-style subagents to save context tokens. Caveman output is ~1/3 size of vanilla prose.

## Subagents
| Subagent | Job | Trigger Phrase |
|---|---|---|
| `cavecrew-investigator` | Locate code | "Where is X", "what calls Y" |
| `cavecrew-builder` | Edit ≤2 files | "Fix X", "change Y" |
| `cavecrew-reviewer` | Review diff | "Review changes", "audit" |

Use vanilla equivalent if prose or architectural rationale is needed.

## Chaining
1. `investigator` finds lines.
2. `builder` applies changes.
3. `reviewer` verifies the diff.
