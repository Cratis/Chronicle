---
agent: agent
description: Scaffold a new feature — folder structure, composition page, routing, and navigation.
---
<!-- cratis-ai-managed: prompts/scaffold-feature.prompt.md -->

# Scaffold a Feature

Scaffold a brand-new feature folder (composition page, routing, navigation) — ready for slices. Follow the Project Layout in `.cratis/ai/rules/application-profile.md` exactly, and Phase 3 of its Implementation Workflow plus the Composition section of `.cratis/ai/rules/react.md` for the composition page and routing. Invoke the **cratis-arc-react-page** skill for the page itself.

## Confirm first

- **Feature name** — PascalCase (e.g. `Projects`, `Invoices`)
- **Route path** — kebab-case (e.g. `/projects`)
- **Navigation label** and **icon** (from `react-icons/md`)

The feature folder lives directly under the app source root (or under an optional `<Module>/`) — there is no top-level `Features/` wrapper. After scaffolding, add behavior with the **new-vertical-slice** prompt. The rules carry the step-by-step detail; don't duplicate it here.
