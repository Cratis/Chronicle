---
agent: agent
description: Review a pull request against all Cratis project standards and produce a structured review report.
---

# Review Pull Request

Produce a structured review of a pull request against all Cratis standards.

## Confirm first

- **PR number or branch**, and the affected repos/projects.

## Process

1. **Gather context** — list and read every changed file; identify the slice type(s).
2. **Architecture & quality** — run the **Code Reviewer** agent (it checks `.ai/rules/` and folds in the performance pass).
3. **Security** — run the **Security Reviewer** agent.
4. **Spec coverage** — confirm each slice has specs (happy path + each failure); confirm tests pass if runnable.
5. **Docs** — confirm public-facing changes updated documentation.

## Output

```
## Pull Request Review — #<number>
### Summary — <2–3 sentences>
### Architecture & Quality — ✅ / ⚠️ / ❌   <findings>
### Security — ✅ / ⚠️ / ❌   <findings>
### Spec Coverage — ✅ / ⚠️ / ❌
### Documentation — ✅ / ⚠️ / ❌
### Overall — ✅ Approved / ⚠️ Approved with comments / ❌ Changes requested
**Blocking** (violates a MUST): 1. …    **Suggestions**: 1. …
```

Be specific — file, line, and corrected code for every blocking issue.
