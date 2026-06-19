---
agent: agent
description: Scaffold a new feature — folder structure, composition page, routing, and navigation.
---

# Scaffold a Feature

Scaffold a brand-new feature folder (composition page, routing, navigation) — ready for slices. Invoke the **scaffold-feature** skill and follow it exactly.

## Confirm first

- **Feature name** — PascalCase (e.g. `Projects`, `Invoices`)
- **Route path** — kebab-case (e.g. `/projects`)
- **Navigation label** and **icon** (from `react-icons/md`)

The feature folder lives directly under the app source root (or under an optional `<Module>/`) — there is no top-level `Features/` wrapper. After scaffolding, add behavior with the **new-vertical-slice** prompt/skill. The skill carries the step-by-step detail; don't duplicate it here.
