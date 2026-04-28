---
name: use-this-keyword
description: 'Enforce using this. qualifier for all instance field, property, and method access in C# classes. Use when writing, reviewing, or editing C# class code to ensure every instance member access is prefixed with this.'
---

# Use `this.` Qualifier in C# Classes

## Rule

Every access to an instance field, property, or method inside a class **must** be qualified with `this.`

- Applies to all reads, writes, and method calls on the current instance.
- No exceptions — not even in constructors, properties, or event handlers.

## When to Apply

- Writing a new C# class, struct, or MonoBehaviour.
- Editing existing C# code that accesses instance members.
- Reviewing code for compliance with this rule.

## Procedure

### 1. Identify Instance Member Access

Scan the class body for any unqualified access to:
- Instance fields (`health`, `data`, `label`)
- Instance properties (`Name`, `IsActive`)
- Instance methods (`Init()`, `Refresh()`, `LoadComponents()`)

### 2. Add `this.` Prefix

Prefix every unqualified instance member access with `this.`:

```csharp
// WRONG
void Start()
{
    health = maxHealth;
    label.text = name;
    Init();
}

// CORRECT
void Start()
{
    this.health = this.maxHealth;
    this.label.text = this.name;
    this.Init();
}
```

### 3. Exceptions — Do NOT Add `this.`

- Local variables and method parameters.
- Static members (use `ClassName.Member` instead).
- Base class calls (`base.Method()`).

### 4. Validate

After writing or editing:
- Confirm no bare field/property/method references remain inside the class body.
- Run a quick scan for common patterns: assignments, condition checks, return statements, method call chains.

## Quick Checklist

- [ ] All field reads use `this.field`
- [ ] All field writes use `this.field = ...`
- [ ] All property reads/writes use `this.Property`
- [ ] All instance method calls use `this.Method()`
- [ ] Local variables and parameters are **not** prefixed
- [ ] Static members are **not** prefixed with `this.`
