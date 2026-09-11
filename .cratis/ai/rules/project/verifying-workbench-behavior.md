---
applyTo: "**/*"
---

## Verifying Workbench behavior

Drive the Workbench through the browser tooling rather than guessing from source — several of its
views are fed by observable (SSE) queries whose behavior only shows up at runtime:

- Sign in first, or every view renders empty and looks like a data bug.
- Inspect the SSE frames when a live view misbehaves. Observable queries default to `Delta`
  transfer mode, so a frame carries a `changeSet` (`added` / `replaced` / `removed`) and an empty
  `data` array — the client reconstructs the collection from the previous state.
- Delta reconciliation matches items by a property named `id` on both the server
  (`ChangeSetComputor`) and the client (`useObservableQuery`). A read model without an `id`
  falls back to whole-payload JSON equality, which is fragile for models carrying a changing
  timestamp. Check this first when a live list grows, duplicates, or fails to drop rows.
