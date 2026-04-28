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

## Code Comments Language
- All code comments (inline, block, and XML doc comments) must be written in **English**.
- Do **not** write comments in Vietnamese or any other language unless the user explicitly requests a translation task.

## Control Flow
- **Never** nest `if` statements. Always prefer early return (guard clauses) to reduce nesting.
- Handle invalid/edge cases first and return early, keeping the happy path at the lowest indentation level.

## Unity Lifecycle Methods Are Call-Only
- `Awake`, `Start`, `OnEnable`, `OnDisable`, `OnDestroy`, `Reset`, and all other Unity lifecycle callbacks **must not** contain inline logic.
- They may **only** contain method calls — no `if`, loops, variable declarations, assignments, or expressions directly inside the body.
- Extract all logic into dedicated `private`/`protected` methods and call those instead.

```csharp
// WRONG
private void Start()
{
    if (data == null) data = ScriptableObject.CreateInstance<CardData>();
    label.text = data.name;
}

// CORRECT
private void Start()
{
    this.InitData();
    this.RefreshLabel();
}
```

## No Runtime GetComponent
- **Never** call `GetComponent` (or any variant: `GetComponentInChildren`, `GetComponentInParent`, etc.) in runtime gameplay methods.
- All component references **must** be pre-wired: declare a `[SerializeField]` field and resolve it inside `LoadComponents()` or `Reset()` (editor-only callbacks).
- `GetComponent` is **only** permitted inside `LoadComponents()`, `Reset()`, and `Awake()` (initialization only).
- A **Ctrl class** (e.g. `Card3DReviewCtrl`) is the single owner of all component links for a given GameObject. Other components on the same object must **not** resolve siblings themselves at runtime; they receive references from the Ctrl class or from the Inspector.

## Controller LoadComponents Pattern
When creating a controller class (any class ending in `Ctrl` that inherits from `SaiBehaviour`), every component link **must** follow this exact pattern:

1. Override `LoadComponents()` and call one dedicated method per component.
2. Each dedicated method is `protected virtual`, named `Load<ComponentType>()`, and uses an early-return null guard.
3. Log a `Debug.LogWarning` with `transform.name + "Load<ComponentType>"` so missing links are visible in the Console.

```csharp
protected override void LoadComponents()
{
    this.LoadCard3D();
    this.LoadCardLoader();
}

protected virtual void LoadCard3D()
{
    if (this.card != null) return;
    this.card = this.GetComponent<Card3D>();
    Debug.LogWarning(transform.name + "LoadCard3D", gameObject);
}

protected virtual void LoadCardLoader()
{
    if (this.loader != null) return;
    this.loader = this.GetComponent<CardLoader>();
    Debug.LogWarning(transform.name + "LoadCardLoader", gameObject);
}
```

- The null guard (`if (this.x != null) return;`) prevents overwriting Inspector-assigned values.
- Marking the method `virtual` allows subclasses to override the wiring if needed.
- Each `[SerializeField]` field must have exactly one corresponding `Load<X>()` method.

## C# Compile Check Before Delivery
- After every code change, **always** run `get_errors` on all modified `.cs` files.
- Do **not** deliver the work until `get_errors` returns zero errors.
- If errors are found, fix them all and run `get_errors` again before presenting the final response.

## Always Use `this.`
- **Always** qualify instance field, property, and method access with `this.` inside a class.
- This applies to all reads, writes, and method calls on the current instance — no exceptions.

## UI Toolkit Element Names
- Every element defined in a `.uxml` file or created in C# via `new VisualElement()` (and any subclass) **must** have a non-empty `name` attribute / `name` property set.
- Use `name` values that are descriptive and unique within their parent container (e.g. `"submit-button"`, `"card-title-label"`).
- Anonymous elements (no `name`) are **not** allowed — they break `Q<T>("name")` queries and make debugging in the UI Debugger impossible.

## Evidence-Based Analysis
- Every analysis finding or conclusion **must** be backed by a code reference.
- Prefer citing function/method names (e.g. `LoadCard3D()`, `SetCardData()`) over line numbers or file paths alone.
- If a conclusion cannot be supported by a concrete code reference, state that explicitly rather than asserting it as fact.

## Response Summary
- After completing every request, always end the response with a summary section titled **"Tổng kết"**.
- Use bullet points to list everything that was done.
- Follow with a second list titled **"Quy tắc đã tuân theo"** that explicitly names each rule from these guidelines that was applied during the task.