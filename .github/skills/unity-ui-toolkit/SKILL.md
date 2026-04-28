---
name: unity-ui-toolkit
description: 'Enforce UI Toolkit naming rules: every VisualElement defined in .uxml or created in C# must have a non-empty name. Use when writing or reviewing UI Toolkit code or .uxml files.'
---

# UI Toolkit Element Naming Rules

## Rule

Every `VisualElement` (and any subclass: `Button`, `Label`, `TextField`, etc.) **must** have a non-empty `name`.

- In `.uxml` files: set the `name` attribute.
- In C#: set the `name` property immediately after creation.

Anonymous elements are **not** allowed — they break `Q<T>("name")` queries and make debugging in the UI Debugger impossible.

## Rules for Name Values

- Descriptive and unique within the parent container.
- Use kebab-case: `"submit-button"`, `"card-title-label"`, `"health-bar-container"`.
- Name must reflect the element's role, not its type (not `"label1"`, `"button2"`).

## In `.uxml` Files

### WRONG
```xml
<ui:Button text="Submit" />
<ui:Label text="Card Title" />
```

### CORRECT
```xml
<ui:Button name="submit-button" text="Submit" />
<ui:Label name="card-title-label" text="Card Title" />
```

## In C#

### WRONG
```csharp
var btn = new Button();
btn.text = "Submit";
root.Add(btn);
```

### CORRECT
```csharp
var btn = new Button();
btn.name = "submit-button";
btn.text = "Submit";
root.Add(btn);
```

## Querying

With names set, `Q<T>()` works reliably:

```csharp
var btn = root.Q<Button>("submit-button");
var label = root.Q<Label>("card-title-label");
```

## Checklist

- [ ] Every `<ui:*>` element in `.uxml` has a `name` attribute.
- [ ] Every `new VisualElement()` (and subclass) in C# has `name` set before being added to the hierarchy.
- [ ] All names are kebab-case and describe the element's role.
- [ ] No two siblings share the same `name`.
- [ ] No name is generic like `"label1"`, `"container"`, `"button"`.
