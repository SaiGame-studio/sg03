---
name: solid-kiss-principles
description: 'Enforce SOLID and KISS design principles when writing or reviewing C# code. Use when creating classes, adding methods, refactoring, or reviewing architecture to ensure code is simple, single-responsibility, open for extension, and not over-engineered.'
---

# SOLID & KISS Principles

## KISS — Keep It Simple, Stupid

**Rule:** Every solution must be as simple as possible. Do not add complexity unless it is directly required.

### KISS Checklist
- [ ] No abstraction that is used only once.
- [ ] No helper method that wraps a single line.
- [ ] No design pattern applied without a concrete recurring need.
- [ ] No parameter/flag that controls behavior that could be two separate methods.
- [ ] Solution can be understood by reading top-to-bottom without jumping files.

### KISS Examples

**WRONG — unnecessary abstraction**
```csharp
public interface ICardNameProvider { string GetName(); }
public class CardNameProvider : ICardNameProvider
{
    public string GetName() => "DefaultCard";
}
```

**CORRECT — direct**
```csharp
private string GetCardName() => "DefaultCard";
```

---

## SOLID Principles

### S — Single Responsibility Principle
Each class does exactly **one** thing. If you can describe what a class does using "and", it violates SRP.

**Checklist:**
- [ ] Class name describes one responsibility (not `CardManagerAndLoader`).
- [ ] Public methods all relate to the same concern.
- [ ] UI, data, and logic are in separate classes.

**WRONG:**
```csharp
public class CardManager
{
    public void LoadFromDisk() { }
    public void RenderCard() { }
    public void SaveScore() { }
}
```

**CORRECT:**
```csharp
public class CardLoader  { public void LoadFromDisk() { } }
public class CardView    { public void Render() { } }
public class ScoreWriter { public void Save() { } }
```

---

### O — Open/Closed Principle
Classes are **open for extension, closed for modification**. Add behavior via inheritance or composition, not by editing existing logic.

**Checklist:**
- [ ] New behavior added by subclassing or adding a new class, not editing existing methods.
- [ ] Existing methods are not modified to add a new `if` branch for a new case.

**WRONG — modifying existing class:**
```csharp
public void Play(string cardType)
{
    if (cardType == "fire") { }
    else if (cardType == "water") { }  // new branch added every time
}
```

**CORRECT — extend via polymorphism:**
```csharp
public abstract class CardEffect { public abstract void Apply(); }
public class FireEffect  : CardEffect { public override void Apply() { } }
public class WaterEffect : CardEffect { public override void Apply() { } }
```

---

### L — Liskov Substitution Principle
A subclass must be usable wherever the base class is used, **without breaking behavior**.

**Checklist:**
- [ ] Overridden methods do not throw `NotImplementedException`.
- [ ] Subclass does not weaken preconditions or strengthen postconditions.
- [ ] Substituting a subclass does not change program correctness.

**WRONG:**
```csharp
public class SpecialCard : Card
{
    public override void Play() => throw new NotImplementedException();
}
```

**CORRECT:** If a subclass cannot implement a method, it should not inherit from that base — use composition instead.

---

### I — Interface Segregation Principle
Clients must not depend on methods they do not use. Prefer **small, focused interfaces** over large general ones.

**Checklist:**
- [ ] No interface with more than 5 methods unless all callers use all methods.
- [ ] Interfaces split by consumer role, not by implementor convenience.

**WRONG:**
```csharp
public interface ICard
{
    void Play();
    void Render();
    void SaveToDatabase();
    void SendAnalytics();
}
```

**CORRECT:**
```csharp
public interface IPlayable  { void Play(); }
public interface IRenderable { void Render(); }
```

---

### D — Dependency Inversion Principle
High-level modules must not depend on low-level modules. Both depend on **abstractions**.

**Checklist:**
- [ ] High-level classes receive dependencies via constructor or `[SerializeField]`, not `new`.
- [ ] Concrete types are not hardcoded inside business logic.
- [ ] Unity components use `[SerializeField]` + interface references where possible.

**WRONG:**
```csharp
public class CardManager
{
    private FileCardLoader loader = new FileCardLoader();
}
```

**CORRECT:**
```csharp
public class CardManager
{
    [SerializeField] private ICardLoader loader;
}
```

---

## Procedure When Writing Code

1. **Before adding a class:** Does it have exactly one responsibility? Name it after that responsibility.
2. **Before adding a method:** Is this the simplest solution? Could it be a direct inline expression?
3. **Before adding an interface:** Will it have more than one implementor? If not, skip the interface.
4. **Before adding inheritance:** Would composition be simpler?
5. **After writing:** Run the KISS and SOLID checklists above.

## Procedure When Reviewing Code

1. Find classes — check SRP: can you describe it without "and"?
2. Find `if/else` chains on type — likely OCP violation, suggest polymorphism.
3. Find `new SomeConcreteClass()` inside business logic — likely DIP violation.
4. Find large interfaces — check ISP: do all callers use all methods?
5. Find subclasses with `throw new NotImplementedException()` — LSP violation.
6. Find single-use abstractions — KISS violation, flatten them.
