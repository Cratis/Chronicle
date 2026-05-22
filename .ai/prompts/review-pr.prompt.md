---
agent: agent
description: Review a pull request against all Cratis project standards and produce a structured review report.
---

# Review Pull Request

I need a structured **code review** of a pull request against all Cratis project standards.

## Inputs

- **PR number or branch name** — so I can examine the changed files
- **Scope** — which repos / projects are affected

## Review process

### Step 1 — Gather context

- List all changed files in the PR
- Read each changed file in full
- Identify the slice type(s) affected (State Change / State View / Automation / Translation)

### Step 2 — Architecture review

Delegate to the `code-reviewer` agent with all changed files.

Check against:
- `.github/instructions/vertical-slices.instructions.md`
- `.github/instructions/csharp.instructions.md`
- `.github/instructions/typescript.instructions.md`
- `.github/instructions/components.instructions.md`
- `.github/instructions/concepts.instructions.md`
- `.github/copilot-instructions.md`

### Step 3 — Security review

Delegate to the `security-reviewer` agent with all changed files.

Check all security categories:
- Input validation & injection
- Auth / authz
- Sensitive data exposure
- Secrets & configuration
- Dependency & serialisation safety
- Event sourcing specifics
- Frontend attack surface

### Step 4 — Spec coverage

- Confirm every State Change command has specs covering happy path + failure cases
- Confirm `dotnet test` output shows no regressions (if available)

### Step 5 — Documentation

- If a new public API, feature, or component was added, confirm documentation was updated
- If a breaking change was introduced, confirm it is called out in the PR description

---

## Output format

```
## Pull Request Review — #<number>

### Summary
<2–3 sentence overview of what the PR does>

### Architecture — ✅ / ⚠️ / ❌
<findings from code-reviewer>

### Security — ✅ / ⚠️ / ❌
<findings from security-reviewer>

### Spec Coverage — ✅ / ⚠️ / ❌
<list of commands checked and whether specs exist>

### Documentation — ✅ / ⚠️ / ❌
<notes on docs updates>

### Overall verdict
**✅ Approved** / **⚠️ Approved with comments** / **❌ Changes requested**

**Blocking issues** (must fix before merge):
1. …

**Suggestions** (non-blocking):
1. …
```

## Conventions reminder

- Blocking issues = anything that violates a `MUST` rule in the instructions
- Suggestions = style improvements, optional enhancements, or `SHOULD` rules
- Be specific: always include file, line number, and corrected code for blocking issues
