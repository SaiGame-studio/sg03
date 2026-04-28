---
name: unity-lifecycle-rules
description: 'Enforce Unity lifecycle method rules: lifecycle callbacks must contain only method calls (no inline logic), and GetComponent must never be called at runtime. Use when writing or reviewing MonoBehaviour or SaiBehaviour scripts.'
---

# Unity Lifecycle & Component Reference Rules

## Rule 1 — Lifecycle Methods Are Call-Only

`Awake`, `Start`, `OnEnable`, `OnDisable`, `OnDestroy`, `Reset`, and all Unity lifecycle callbacks **must not** contain inline logic.

They may **only** contain method calls. No `if`, loops, variable declarations, assignments, or expressions directly inside the body.

### WRONG
```csharp
private void Start()
{
    if (this.data == null) this.data = ScriptableObject.CreateInstance<CardData>();
    this.label.text = this.data.name;
}
```

### CORRECT
```csharp
private void Start()
{
    this.InitData();
    this.RefreshLabel();
}

private void InitData()
{
    if (this.data != null) return;
    this.data = ScriptableObject.CreateInstance<CardData>();
}

private void RefreshLabel()
{
    this.label.text = this.data.name;
}
```

### Checklist
- [ ] `Awake` body contains only method calls.
- [ ] `Start` body contains only method calls.
- [ ] `OnEnable` / `OnDisable` bodies contain only method calls.
- [ ] No `if`, loop, or assignment directly inside any lifecycle method body.
- [ ] All extracted methods are `private` or `protected`, named after what they do.

---

## Rule 2 — No Runtime GetComponent

**Never** call `GetComponent` (or `GetComponentInChildren`, `GetComponentInParent`, etc.) in runtime gameplay methods.

All component references **must** be pre-wired via `[SerializeField]` and resolved in `LoadComponents()` or `Reset()`.

`GetComponent` is **only** permitted inside:
- `LoadComponents()`
- `Reset()`
- `Awake()` (initialization only)

### WRONG
```csharp
private void Update()
{
    var rb = this.GetComponent<Rigidbody>();
    rb.AddForce(Vector3.up);
}
```

### CORRECT
```csharp
[SerializeField] private Rigidbody rb;

protected virtual void LoadRigidbody()
{
    if (this.rb != null) return;
    this.rb = this.GetComponent<Rigidbody>();
    Debug.LogWarning(transform.name + "LoadRigidbody", gameObject);
}

private void ApplyForce()
{
    this.rb.AddForce(Vector3.up);
}
```

### Checklist
- [ ] No `GetComponent` call inside `Update`, `FixedUpdate`, `LateUpdate`.
- [ ] No `GetComponent` call inside any gameplay-triggered method.
- [ ] Every component reference declared as `[SerializeField]`.
- [ ] `GetComponent` only appears in `LoadComponents()`, `Reset()`, or `Awake()`.
- [ ] Ctrl class owns all component links — sibling components do not resolve each other at runtime.
