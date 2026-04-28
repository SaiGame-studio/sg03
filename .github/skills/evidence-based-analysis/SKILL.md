---
name: evidence-based-analysis
description: 'Enforce evidence-based analysis: every finding, conclusion, or claim must cite a code reference. Prefer function/method names as citations. Use when analyzing code, reviewing architecture, explaining behavior, or summarizing findings.'
---

# Evidence-Based Analysis

## Rule

Every analysis finding or conclusion **must** be backed by a concrete code reference.

- Prefer **function/method names** as the primary citation form (e.g. `LoadCard3D()`, `SetCardData()`).
- Fall back to **class/field names** when no function is directly relevant.
- If a conclusion cannot be supported by a concrete code reference, state that explicitly — do **not** assert it as fact.

## When to Apply

- Analyzing how a system or feature works.
- Reviewing architecture, data flow, or dependencies.
- Explaining why a bug occurs or how a fix works.
- Summarizing what a class, module, or file does.
- Any response that contains the words "it does", "this means", "therefore", "because", "since", "the reason is".

## Citation Priority (highest → lowest)

| Priority | Form | Example |
|----------|------|---------|
| 1 | Method/function name | `LoadCard3D()`, `OnCardClicked()` |
| 2 | Class or struct name | `CardData`, `Card3DReviewCtrl` |
| 3 | Field or property name | `this.card`, `this.loader` |
| 4 | File + line range | [Card3DReviewCtrl.cs](../Assets/_sg03/Card3DReviewCtrl.cs#L12-L18) |

Never use line numbers alone without a method name or file link alongside them.

## Procedure

### 1. Make the Finding

State the conclusion clearly in one sentence.

### 2. Attach the Citation Immediately

Follow the finding with the supporting reference on the very next line or inline. Do not separate the claim from its evidence.

**Format:**
> [Finding]. Evidence: `MethodName()` [brief reason why this method is the evidence].

### 3. If No Evidence Exists

Write explicitly:
> "No direct code reference found for this claim — treating as unverified."

Do **not** omit the disclaimer or assert the finding as certain.

## Examples

### WRONG — Unsupported assertion
> The card loader initializes before the card data is set.

### CORRECT — Supported by method citation
> The card loader initializes before the card data is set. Evidence: `LoadComponents()` calls `LoadCardLoader()` before any `SetCardData()` call is made.

---

### WRONG — Vague reference
> The controller handles all component wiring (see line 45).

### CORRECT — Named reference
> The controller handles all component wiring. Evidence: `LoadComponents()` overrides the base class and delegates to `LoadCard3D()` and `LoadCardLoader()`.

---

### WRONG — Claim without evidence
> This system uses a singleton pattern.

### CORRECT — Explicit uncertainty
> This system may use a singleton pattern — no direct code reference found for this claim, treating as unverified.

## Quick Checklist

- [ ] Every finding has an inline citation.
- [ ] Citations use method/function names where possible.
- [ ] No conclusion uses "it does X" without naming the method that does X.
- [ ] Unverifiable claims are explicitly labeled as unverified.
- [ ] Line numbers are never cited alone without a method name or file link.
