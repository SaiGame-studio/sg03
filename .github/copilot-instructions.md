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

## Split Large Logic Files
- Any file containing code logic that exceeds 1000 lines must be reviewed for a reasonable split into smaller files.
- Prefer extracting cohesive responsibilities into dedicated classes, partial classes, page controllers, models, services, or helper types that follow existing project patterns.
- Do **not** add new logic to a file already over 1000 lines unless splitting is impossible or the user explicitly approves keeping it together.
- Keep each extracted type in its own file and preserve behavior while reducing file size.

## Unity Inspector Buttons
- **Never** use `[ContextMenu]` to expose buttons.
- Always create Inspector buttons via a dedicated `[CustomEditor]` script.

## Editor Scripts Must Live in an `Editor` Folder
- Any script that uses `UnityEditor` APIs (e.g. `CustomEditor`, `EditorWindow`, `PropertyDrawer`, `MenuItem`) **must** be placed inside a folder named exactly `Editor` (e.g. `Assets/_sg03/SomeFeature/Editor/MyEditor.cs`).
- **Never** place Editor-only scripts outside an `Editor` folder — Unity strips `UnityEditor` from runtime builds and the build will fail.
- The `Editor` folder can be at any depth under `Assets/_sg03/`, but the folder name must be exactly `Editor` (case-sensitive).

## Code Comments Language
- All code comments (inline, block, and XML doc comments) must be written in **English**.
- Do **not** write comments in Vietnamese or any other language unless the user explicitly requests a translation task.

## Control Flow
- **Never** nest `if` statements. Always prefer early return (guard clauses) to reduce nesting.
- Handle invalid/edge cases first and return early, keeping the happy path at the lowest indentation level.

## Unity Lifecycle Methods Are Call-Only
- Lifecycle callbacks (`Awake`, `Start`, `OnEnable`, `OnDisable`, `OnDestroy`, `Reset`) may **only** contain method calls — no inline logic, `if`, loops, or assignments.
- See skill: `unity-lifecycle-rules`.

## No Runtime GetComponent
- **Never** call `GetComponent` in runtime methods. Pre-wire all references via `[SerializeField]` in `LoadComponents()`.
- See skill: `unity-lifecycle-rules`.

## Controller LoadComponents Pattern
- Ctrl classes (`SaiBehaviour` subclasses ending in `Ctrl`) must follow the `LoadComponents()` + `Load<X>()` pattern with null guard and `Debug.LogWarning`.
- Do not aggregate multiple unrelated classes into one generic loader (for example `LoadManagers`, `LoadAll`, `LoadChildComponents`).
- Each `[SerializeField]` reference must have its own dedicated `Load<X>()` method (one reference -> one loader method).
- **Never resolve a reference via a Singleton** (e.g. `SaiServer.Instance`) inside any `Load<X>()` method. Use scene hierarchy APIs only (`GetComponent`, `GetComponentInChildren`, `GetComponentInParent`, `FindFirstObjectByType`).
- See skill: `unity-ctrl-pattern`.

## C# Compile Check Before Delivery
- After every code change, **always** run `get_errors` on all modified `.cs` files.
- Do **not** deliver the work until `get_errors` returns zero errors.
- If errors are found, fix them all and run `get_errors` again before presenting the final response.

## Always Use `this.`
- **Always** qualify instance field, property, and method access with `this.` inside a class. No exceptions.
- See skill: `use-this-keyword`.

## UI Toolkit Element Names
- Every `VisualElement` in `.uxml` or C# **must** have a non-empty `name`. Anonymous elements are not allowed.
- See skill: `unity-ui-toolkit`.

## No SaiGame.UI Dependency
- Code under `Assets/_sg03/` must **not** use the `SaiGame.UI` namespace.
- Do **not** add `using SaiGame.UI;` in `_sg03` scripts.
- Do **not** inherit from or depend on UI classes located under `Assets/SaiGame/UI/`.
- If `_sg03` needs UI routing, panels, or helpers, create dedicated implementations under `Assets/_sg03/` using the `SG03.UI` namespace.

## Single Responsibility — Method Level
- Every method must do **exactly one thing**. If you can describe what it does using "and" or "then", split it.
- If a method has two or more distinct loops, phases, or concerns, extract each into its own named method.
- Coroutine coordinators must contain only `yield return` calls — no inline logic.
- If two methods share identical logic, extract the shared part and reuse it (DRY).
- See skill: `single-responsibility-method`.

## Evidence-Based Analysis
- Every finding or conclusion **must** cite a code reference. Prefer method names. State explicitly if no reference exists.
- See skill: `evidence-based-analysis`.

## Response Summary
- After completing every request, always end the response with a summary section titled **"Tổng kết"**.
- Use bullet points to list everything that was done.
- Follow with a second list titled **"Quy tắc đã tuân theo"** that explicitly names each rule from these guidelines that was applied during the task.