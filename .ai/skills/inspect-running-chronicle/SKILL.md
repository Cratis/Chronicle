---
name: inspect-running-chronicle
description: Inspect or operate a running Chronicle server with the cratis CLI - list failed partitions, read why an observer is stuck, replay a partition, browse events, event types, read models, projections and jobs. Use when a question is about the state of a live store rather than the source code, when a projection will not update, when an observer is quarantined, or when checking whether an event was actually appended. Also use when setting the CLI up for a project so agents can reach the store.
---

# Inspect a Running Chronicle

Source code says what *should* happen. When the question is what *is* happening in a live store — a projection that will not move, an observer that stopped, an event you are not sure was appended — read the server instead of the code. The `cratis` CLI is how.

**Do not guess command names from this file.** The CLI ships its own complete, versioned catalog and that is the authority:

```bash
cratis llm-context          # every command, option and argument as JSON
cratis <group> --help       # the same, one group at a time
```

This skill covers *when to reach for the CLI and how to read what comes back*. The catalog covers *what to type*.

## Setting it up in a project

Once per project, so every agent working it can reach the store:

```bash
cratis init                 # detects the AI tools in use and writes CHRONICLE.md + a chronicle-cli skill
cratis init --refresh       # re-capture after upgrading the CLI
```

Two things worth knowing before running it:

- The embedded catalog is a **snapshot**, not a live lookup. After a CLI upgrade it still describes the older surface; `cratis init` reports the mismatch and `--refresh` fixes it.
- If the repository's instruction file (`AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`) is **generated from a shared corpus and propagated** — as it is in every repository consuming this one — pass `--no-context`. Appending to a generated file works until the next sync silently removes it. Add the `@CHRONICLE.md` line to the canonical source instead.

## Reaching the right server

Resolution order is `--server` → `CHRONICLE_CONNECTION_STRING` → the active context → `chronicle://localhost:35000`. Prefer a named context over repeating a connection string:

```bash
cratis context create dev --server chronicle://localhost:35000
cratis context set dev
```

**Be deliberate about which store you are pointed at.** The same commands read production and a local container, and several of them are destructive. Confirm the context before running anything that writes.

## When to reach for it

| Question | Where to look |
|---|---|
| Why has this read model stopped updating? | failed partitions for its observer — the error is on the partition, not in the log |
| An observer is "quarantined" — why? | the failed partition's detail, with full stack traces |
| Did this event actually get appended? | the event sequence, filtered by event type or event source |
| What is this event's shape in the store? | the registered event types |
| Is this projection even registered? | the projections list, then its definition |
| Is a replay or migration still running? | the jobs list |

## Reading what comes back

- **A failed partition does not retry itself.** It stays failed until something clears it, so a stale value is permanent rather than slow. That distinction is the whole diagnosis: "not arrived yet" and "will never arrive" look identical from the outside.
- **Fix the cause before replaying.** Replaying into an unfixed handler fails the same way and buries the original error under a newer one.
- **Prefer `--output plain` for large listings** (events, event types, read models, projections) — the JSON repeats every field name on every row. Use `--output json` or `json-compact` for `show`/`get` commands where you need the nested structure.
- **`--quiet` prints identifiers only**, which is what you want when piping one command into another.

## Before you change anything

Destructive commands — replay, retry, remove, clearing a quarantine — prompt for confirmation in a terminal and take `--yes` in scripts. Reaching for `--yes` to silence a prompt you have not read is how the wrong store gets replayed.

A failed partition that you have not yet explained is not a thing to clear. Read it, fix the handler, then replay. Clearing it first destroys the evidence and the same failure returns on the next event.

## Related

- **diagnose-slice** — symptom → cause → owning rule, for when the defect is in the code rather than the store's state. Start there when the symptom is reproducible locally; start here when it is only visible on a running server.
- **observable-query-curl** — for exercising an application's own observable query endpoints over HTTP, which is a different surface from the store's management API.
