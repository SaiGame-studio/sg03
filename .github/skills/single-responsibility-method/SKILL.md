---
name: single-responsibility-method
description: 'Enforce Single Responsibility at the method level: every method does exactly one thing. If a method can be split into independent concerns, it must be split. Use when writing, reviewing, or refactoring any C# method that contains multiple distinct steps or responsibilities.'
---

# Single Responsibility — Method Level

## Rule

Every method must do **exactly one thing**.
If you can describe what a method does using "and" or "then", it must be split.

---

## When to Split

Split a method when it contains **two or more independent concerns**, such as:

- Fetching data **and** processing it
- Validating input **and** applying it
- Spawning object A **and** spawning object B
- Setting up state **and** notifying listeners
- One loop that does task X **followed by** another loop that does task Y

---

## Checklist

- [ ] The method name describes a single action (not `DoXAndY`, `SetupAndLoad`, `SpawnAllAndNotify`).
- [ ] The method body has only **one level of abstraction** — either it coordinates calls, or it does low-level work, never both.
- [ ] If the method has two `for`/`foreach` loops over different data, each loop is a separate method.
- [ ] If the method has two distinct phases (e.g. "first spawn source, then spawn hand"), each phase is a separate method.
- [ ] Extracted methods are named precisely after their single responsibility.

---

## Examples

### WRONG — method does multiple things

```csharp
private IEnumerator SpawnResumeRoutine()
{
    // phase 1: spawn alpha source
    for (int i = 0; i < alphaCount; i++) { ... yield return null; }

    // phase 2: spawn omega source
    for (int i = 0; i < omegaCount; i++) { ... yield return null; }

    // phase 3: spawn alpha hand
    for (int i = 0; i < handSlots.Length; i++) { ... yield return null; }
}
```

### CORRECT — each concern is its own method

```csharp
private IEnumerator SpawnResumeRoutine()
{
    yield return this.SpawnAlphaSourceRoutine();
    yield return this.SpawnOmegaSourceRoutine();
    yield return this.SpawnAlphaHandResumeRoutine();
}

private IEnumerator SpawnAlphaSourceRoutine() { ... }
private IEnumerator SpawnOmegaSourceRoutine() { ... }
private IEnumerator SpawnAlphaHandResumeRoutine() { ... }
```

---

## Naming Extracted Methods

| What it does | Good name |
|---|---|
| Spawns cards at alpha source positions | `SpawnAlphaSourceRoutine()` |
| Spawns cards at omega source positions | `SpawnOmegaSourceRoutine()` |
| Spawns alpha hand on resume | `SpawnAlphaHandResumeRoutine()` |
| Validates then applies output | Split into `Validate()` + `ApplyOutput()` |

---

## Coroutine Coordination Pattern

When a coroutine must sequence multiple phases, the top-level routine is a **coordinator only** — it contains only `yield return` calls, no logic:

```csharp
private IEnumerator SpawnResumeRoutine()
{
    yield return this.SpawnAlphaSourceRoutine();
    yield return this.SpawnOmegaSourceRoutine();
    yield return this.SpawnAlphaHandResumeRoutine();
    this.spawnRoutine = null;
}
```

Each sub-routine handles its own guard checks (`if (prefab == null) yield break`).

---

## DRY — Don't Repeat Logic

If two methods share the same loop body (e.g. `SpawnGameStartRoutine` and `SpawnGameResumeRoutine` both spawn alpha + omega source), extract the shared logic into one method and call it from both:

```csharp
private IEnumerator SpawnGameStartRoutine()
{
    yield return this.SpawnAlphaSourceRoutine();
    yield return this.SpawnOmegaSourceRoutine();
    this.spawnRoutine = null;
}

private IEnumerator SpawnGameResumeRoutine()
{
    yield return this.SpawnAlphaSourceRoutine();   // reused
    yield return this.SpawnOmegaSourceRoutine();   // reused
    yield return this.SpawnAlphaHandResumeRoutine();
    this.spawnRoutine = null;
}
```
