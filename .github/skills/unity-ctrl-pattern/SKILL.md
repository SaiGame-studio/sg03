---
name: unity-ctrl-pattern
description: 'Enforce the LoadComponents pattern for Unity Ctrl classes (SaiBehaviour subclasses ending in Ctrl). Use when creating or reviewing any controller class that needs to wire component references.'
---

# Unity Controller LoadComponents Pattern

## When to Use

Any class that:
- Inherits from `SaiBehaviour`
- Has a name ending in `Ctrl` (e.g. `Card3DReviewCtrl`, `CardLoaderCtrl`)
- Needs to hold references to sibling or child components

## Required Pattern

Every component link **must** follow these exact rules:

1. Override `LoadComponents()` and call one dedicated method per component.
2. Each dedicated method is `protected virtual`, named `Load<ComponentType>()`.
3. Each method uses an **early-return null guard** to avoid overwriting Inspector-assigned values.
4. Each method logs a `Debug.LogWarning` with `transform.name + "Load<ComponentType>"`.

```csharp
[SerializeField] private Card3D card;
[SerializeField] private CardLoader loader;

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

## Rules

- The null guard `if (this.x != null) return;` is mandatory — prevents overwriting Inspector values.
- `virtual` is mandatory — allows subclasses to override wiring.
- One `[SerializeField]` field → exactly one `Load<X>()` method. No exceptions.
- Do not use a generic aggregator method to wire multiple different class references (for example `LoadManagers`, `LoadAll`, `LoadChildComponents`).
- `LoadComponents()` may orchestrate many `Load<X>()` calls, but each `Load<X>()` must assign only one reference type.
- Sibling components must **not** resolve each other. The Ctrl class is the single owner of all links.
- `GetComponent` is only called inside `Load<X>()` — never anywhere else.

## Checklist

- [ ] Class inherits from `SaiBehaviour` and name ends in `Ctrl`.
- [ ] `LoadComponents()` is overridden and calls one `Load<X>()` per field.
- [ ] Every `Load<X>()` is `protected virtual`.
- [ ] Every `Load<X>()` has a null guard as the first line.
- [ ] Every `Load<X>()` has a `Debug.LogWarning` after the `GetComponent` call.
- [ ] No generic loader method that assigns multiple unrelated references in one method.
- [ ] No `GetComponent` call outside a `Load<X>()` method.
- [ ] No sibling component resolves its own references at runtime.

## WRONG — Direct wiring without pattern

```csharp
protected override void LoadComponents()
{
    this.card = this.GetComponent<Card3D>();       // no null guard
    this.loader = this.GetComponent<CardLoader>(); // no LogWarning, not virtual
}
```

## WRONG — Aggregating many classes into one loader

```csharp
[SerializeField] private ProfileManager profileManager;
[SerializeField] private CardDataManager cardDataManager;

protected virtual void LoadManagers()
{
    if (this.profileManager == null)
        this.profileManager = this.GetComponentInChildren<ProfileManager>(true);

    if (this.cardDataManager == null)
        this.cardDataManager = this.GetComponentInChildren<CardDataManager>(true);
}
```

## CORRECT — One reference, one loader

```csharp
protected override void LoadComponents()
{
    this.LoadProfileManager();
    this.LoadCardDataManager();
}

protected virtual void LoadProfileManager()
{
    if (this.profileManager != null) return;
    this.profileManager = this.GetComponentInChildren<ProfileManager>(true);
    Debug.LogWarning(transform.name + "LoadProfileManager", gameObject);
}

protected virtual void LoadCardDataManager()
{
    if (this.cardDataManager != null) return;
    this.cardDataManager = this.GetComponentInChildren<CardDataManager>(true);
    Debug.LogWarning(transform.name + "LoadCardDataManager", gameObject);
}
```

## CORRECT — Full pattern

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
