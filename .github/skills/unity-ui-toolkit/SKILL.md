---
name: unity-ui-toolkit
description: 'Enforce UI Toolkit architecture and naming rules: divide-and-conquer layout structure, separate .uxml files per content page, MonoBehaviour-based page controllers dragged into Hierarchy, menu-driven content loading. Use when designing, writing, or reviewing any UI Toolkit screen, panel, or content page.'
---

# UI Toolkit Architecture & Naming Rules

---

## Architecture Rule 1 — Divide and Conquer Layout

Every UI screen must be broken into **separate `.uxml` files**, one per content page. Do **not** put the entire UI in a single `.uxml` file.

### Folder Structure

```
Assets/_sg03/UI/
  MainScreen/
    MainScreen.uxml          ← root shell (menu bar + content container only)
    MainScreen.cs            ← MonoBehaviour, attached to Hierarchy GameObject
  Pages/
    HomePageContent/
      HomePageContent.uxml
      HomePageContent.cs     ← MonoBehaviour, attached to Hierarchy GameObject
    ShopPageContent/
      ShopPageContent.uxml
      ShopPageContent.cs
    SettingsPageContent/
      SettingsPageContent.uxml
      SettingsPageContent.cs
```

**Rules:**
- Each content page lives in its own subfolder.
- The folder name matches the `.uxml` file name and the `.cs` file name exactly.
- The root shell `.uxml` contains only the menu/navigation and an empty `content-container`. It never embeds page content directly.

---

## Architecture Rule 2 — MonoBehaviour-Based Page Controllers

Every C# class that links to a UI content page **must** be a `MonoBehaviour` (or `SaiBehaviour`) so it can be attached to a GameObject and dragged into the Inspector.

**Forbidden:** Plain C# classes (`class MyPage { }`) that reference or instantiate UI content. These cannot be wired via Inspector and break the Hierarchy-based workflow.

### WRONG — plain class, not attachable
```csharp
public class HomePageContent
{
    private VisualTreeAsset uxml;
    public void Show() { }
}
```

### CORRECT — MonoBehaviour, drag into Hierarchy
```csharp
public class HomePageContent : SaiBehaviour
{
    [SerializeField] private VisualTreeAsset uxml;

    public void Show(VisualElement container)
    {
        VisualElement root = this.uxml.Instantiate();
        container.Add(root);
    }
}
```

---

## Architecture Rule 3 — Menu-Driven Content Loading

The root screen controller listens to menu/tab selection and loads the corresponding content page by calling `Show()` on the page controller held as a `[SerializeField]`.

**Rules:**
- The root controller holds `[SerializeField]` references to all page controllers.
- Switching pages: clear the `content-container`, then call `Show()` on the selected page.
- Page controllers are dragged into the root controller's Inspector slots — never instantiated with `new` or `FindObjectOfType`.

### CORRECT — root controller pattern
```csharp
public class MainScreenCtrl : SaiBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private HomePageContent homePage;
    [SerializeField] private ShopPageContent shopPage;
    [SerializeField] private SettingsPageContent settingsPage;

    private VisualElement contentContainer;

    protected override void LoadComponents()
    {
        this.LoadUIDocument();
    }

    protected virtual void LoadUIDocument()
    {
        if (this.uiDocument != null) return;
        this.uiDocument = this.GetComponent<UIDocument>();
        Debug.LogWarning(transform.name + "LoadUIDocument", gameObject);
    }

    private void ShowPage(SaiBehaviour page)
    {
        this.contentContainer.Clear();
        // each page exposes Show(VisualElement container)
    }
}
```

---

## Architecture Rule 4 — Hierarchy Presence Required

Every content page that can appear in UI Toolkit **must** exist as a GameObject in the Hierarchy with its `MonoBehaviour` attached.

**Never** create a content page purely in code at runtime without a corresponding Hierarchy entry.

### Checklist — Architecture
- [ ] Root shell `.uxml` contains only navigation + empty `content-container`.
- [ ] Each content page has its own subfolder, `.uxml`, and `.cs` file.
- [ ] Every page controller is a `MonoBehaviour`/`SaiBehaviour` — no plain C# classes.
- [ ] Every page controller is present as a GameObject in the Hierarchy.
- [ ] Root controller wires page controllers via `[SerializeField]` — no `new`, no `FindObjectOfType`.
- [ ] Switching pages clears the container before loading new content.

---

## Naming Rule — Element Names

Every `VisualElement` (and any subclass: `Button`, `Label`, `TextField`, etc.) **must** have a non-empty `name`.

- In `.uxml` files: set the `name` attribute.
- In C#: set the `name` property immediately after creation.

Anonymous elements are **not** allowed — they break `Q<T>("name")` queries and make debugging in the UI Debugger impossible.

**Name rules:** kebab-case, descriptive, unique within parent (`"submit-button"`, `"card-title-label"`). Never generic (`"label1"`, `"button"`).

### WRONG
```xml
<ui:Button text="Submit" />
```

### CORRECT
```xml
<ui:Button name="submit-button" text="Submit" />
```

### Checklist — Naming
- [ ] Every `<ui:*>` element in `.uxml` has a `name` attribute.
- [ ] Every `new VisualElement()` in C# has `name` set before being added to the hierarchy.
- [ ] All names are kebab-case and describe the element's role.
- [ ] No two siblings share the same `name`.
