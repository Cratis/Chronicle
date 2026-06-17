---
agent: agent
description: Scaffold a complete vertical slice (backend + specs + frontend) for a Cratis-based project.
---

# New Vertical Slice

Implement a complete **vertical slice** end-to-end. Invoke the **new-vertical-slice** skill and follow it exactly; for a full backend + specs + frontend slice you may hand the work to the **Slice Implementer** agent.

## Confirm first

- **Module / Feature** and **slice name**
- **Slice type** — `State Change` / `State View` / `Automation` / `Translation`
- **Behavior** in one sentence, plus the command/query properties and their concept types

## How it runs

Backend → build (Debug + Release) → specs (the in-process `*Scenario` family) → frontend → compose/route, with each quality gate green before the next phase. The authoritative rules are `.ai/rules/general.md` and `.ai/rules/vertical-slices.md`; the skill carries the step-by-step detail. Do not duplicate that detail here.
