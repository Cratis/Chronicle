---
name: chronicle-cli
description: How to use the Cratis CLI (`cratis`) to inspect, diagnose, and manage a Chronicle event-sourced system from the command line or as an AI agent tool. Use this skill whenever the user asks about using the CLI, running `cratis` commands, querying event stores or event types, listing or replaying observers, inspecting read models, checking failed partitions, diagnosing observer errors, configuring CLI connections, switching named contexts, choosing output formats, or integrating the Cratis CLI into AI agent workflows. Also trigger when the user mentions `cratis`, event store CLI, observer CLI, or wants to run any Chronicle operation from a terminal rather than the Workbench UI.
---

# Using the Cratis CLI

The Cratis CLI (`cratis`) is a .NET global tool for managing Chronicle event-sourced systems over gRPC. It covers the full surface: event stores, namespaces, event types, events, observers (reactors, reducers, projections), read models, failed partitions, recommendations, identities, users, applications, and authentication.

The CLI is designed for both human operators and AI agents. Every command produces structured output in multiple formats, errors are predictable, and the entire command surface is self-describing via `cratis llm-context`.

---

## Installation

```shell
dotnet tool install --global Cratis.Chronicle.Cli
```

Verify: `cratis --version`. Update: `dotnet tool update --global Cratis.Chronicle.Cli`.

---

## Connection Setup

The CLI resolves which server to connect to using this precedence (highest wins):

1. `--server` flag on the command
2. `CHRONICLE_CONNECTION_STRING` environment variable
3. Active named context in the config file
4. Default: `chronicle://localhost:35000`

### Connection string format

```text
chronicle://<client-id>:<client-secret>@<host>:<port>/?disableTls=true
```

### Quick local setup

```shell
cratis config set server "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000/?disableTls=true"
cratis config set event-store MyStore
```

After this, most commands work without extra flags.

---

## Output Formats

Every command supports `-o` / `--output` with four values:

| Format | Flag | Use when |
| ------ | ---- | -------- |
| `text` | `-o text` | Interactive terminal use — renders rich tables. Default in most terminals. |
| `plain` | `-o plain` | Piping to `grep`/`awk`/`cut`, or AI agent use — tab-separated, minimal tokens. |
| `json` | `-o json` | Programmatic parsing — indented JSON with full schemas and metadata. |
| `json-compact` | `-o json-compact` | Same as `json` but non-indented — ~30% fewer tokens. |

### Choosing the right format

- **For listing commands** (event-stores, namespaces, event-types, observers, read-models, projections, failed-partitions, recommendations, identities): always use `plain`. It is 10-34x smaller than JSON because JSON includes full JSON Schema blobs, causation chains, and deeply nested event context.
- **For detail commands** (config show, read-models get, observers show, event-types show, projections show, read-models snapshots, auth status, failed-partitions show): use `json` or `json-compact` because the response is structured data you need to parse.
- **For boolean checks** (events has): use `plain` — it returns just `true` or `false`.
- **For confirmations** (replay, retry, context create/set/delete, login, logout): use `plain` — a simple confirmation message.

### Auto-detection

When `-o` is omitted, the CLI auto-detects: `plain` if `NO_COLOR` is set or the terminal lacks ANSI support, otherwise `text`.

### Error output

In `json` and `json-compact` modes, errors are structured:
```json
{"error": "Observer not found", "suggestion": "Run 'cratis observers list' to see available observers"}
```
In `text` and `plain` modes, errors are human-readable colored output. This means it is always safe to parse JSON output programmatically — no unstructured text sneaks in.

---

## Command Reference

### Global options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `--server` | string | Chronicle server connection string |
| `-o, --output` | string | Output format: `json`, `text`, `plain`, or `json-compact` |

### Event store options

Most commands that operate within an event store accept:

| Option | Type | Default |
| ------ | ---- | ------- |
| `-e, --event-store` | string | `default` (or config value) |
| `-n, --namespace` | string | `default` (or config value) |

### Command groups

**event-stores** — `cratis event-stores list`

**namespaces** — `cratis namespaces list [-e Store]`

**event-types**
- `cratis event-types list` — list registered event types
- `cratis event-types show <TYPE>` — show registration with JSON schema. Accepts `Name` or `Name+Generation`.

**events**
- `cratis events get` — query events with `--from`, `--to`, `--event-source-id`, `--event-type`, `--sequence`
- `cratis events count` — get the tail (highest) sequence number. Not a count — gaps may exist.
- `cratis events has <EVENT_SOURCE_ID>` — check if events exist for an event source ID

**observers** — reactors, reducers, and projections
- `cratis observers list [--type reactor|reducer|projection]`
- `cratis observers show <OBSERVER_ID>` — observer IDs are dotted type names (e.g. `Core.MyFeature.Listing.MyProjection`)
- `cratis observers replay <OBSERVER_ID>`
- `cratis observers replay-partition <OBSERVER_ID> <PARTITION>`
- `cratis observers retry-partition <OBSERVER_ID> <PARTITION>`

**failed-partitions**
- `cratis failed-partitions list [--observer <ID>]`
- `cratis failed-partitions show <OBSERVER_ID> <PARTITION>` — includes error messages and stack traces

**projections**
- `cratis projections list`
- `cratis projections show <IDENTIFIER>`

**read-models**
- `cratis read-models list`
- `cratis read-models instances <CONTAINER> [--page N --page-size N]` — page is 0-based
- `cratis read-models get <CONTAINER> <KEY>`
- `cratis read-models occurrences <TYPE>` — replay history
- `cratis read-models snapshots <CONTAINER> <KEY>`

**recommendations**
- `cratis recommendations list`
- `cratis recommendations perform <ID>`
- `cratis recommendations ignore <ID>`

**identities** — `cratis identities list`

**auth** — `cratis auth status`

**login / logout** (top-level commands)
- `cratis login <USERNAME> [--password P@ss]` — without `--password`, prompts interactively
- `cratis logout`

**users**
- `cratis users list`
- `cratis users add <USERNAME> <EMAIL> <PASSWORD>`
- `cratis users remove <USER_ID>`

**applications** (OAuth clients)
- `cratis applications list`
- `cratis applications add <CLIENT_ID> <CLIENT_SECRET>`
- `cratis applications remove <APP_ID>`
- `cratis applications rotate-secret <APP_ID> <NEW_SECRET>`

**context** (named connection profiles)
- `cratis context list`
- `cratis context create <NAME> --server <CONN> [-e Store] [-n Namespace]`
- `cratis context set <NAME>` — switch active context
- `cratis context show`
- `cratis context delete <NAME>` — cannot delete the active context
- `cratis context rename <OLD> <NEW>`

**config**
- `cratis config show`
- `cratis config set <KEY> <VALUE>` — keys: `server`, `event-store`, `namespace`, `client-id`, `client-secret`
- `cratis config path`

**llm-context** — `cratis llm-context` — outputs entire CLI surface as JSON for AI agent tool definitions

---

## AI Agent Integration

The CLI is designed to be called directly by AI agents as a tool interface to Chronicle systems.

### Bootstrap: get the full CLI surface

```shell
cratis llm-context
```

This outputs a single JSON document describing every command, option, example, and per-command output format recommendation. Feed it into your agent's tool definitions or system prompt. Save it once and reference it — the output is stable across sessions.

### Configure once, run many

Set defaults so every subsequent command works without connection flags:

```shell
cratis config set server "chronicle://client:secret@host:35000/?disableTls=true"
cratis config set event-store MyStore
```

Or use the environment variable:

```shell
export CHRONICLE_CONNECTION_STRING="chronicle://client:secret@host:35000"
```

### Token efficiency matters

The difference between output formats is dramatic for AI agents under token budgets:

| Command | Plain | JSON | Ratio |
| ------- | ----- | ---- | ----- |
| `event-types list` | ~1.2 KB | ~41 KB | 34x |
| `events get` (73 events) | ~6.8 KB | ~169 KB | 25x |
| `read-models list` | ~1.5 KB | ~40 KB | 27x |

**Rule of thumb:** use `-o plain` for everything unless you specifically need structured JSON data. The JSON includes raw JSON Schema blobs, causation chains with machine metadata, and deeply nested event context — valuable for deep inspection but wasteful for overview queries.

### Non-interactive use

All commands work non-interactively except `cratis login` without `--password`. For automated pipelines:

```shell
cratis login admin --password "${CHRONICLE_PASSWORD}"
```

Or use client credentials in the connection string to skip the login step entirely.

---

## Common Workflows

### Discover what's in the system

```shell
cratis event-stores list -o plain
cratis namespaces list -e MyStore -o plain
cratis event-types list -e MyStore -o plain
cratis observers list -e MyStore -o plain
cratis read-models list -e MyStore -o plain
```

### Diagnose a failing observer

1. Find which observers have issues:
   ```shell
   cratis observers list -e MyStore --type reactor -o plain
   ```
2. Check failed partitions:
   ```shell
   cratis failed-partitions list -e MyStore -o plain
   ```
3. Get error details:
   ```shell
   cratis failed-partitions show Core.MyFeature.MyReactor abc-123 -e MyStore -o json
   ```
4. After fixing the code, retry:
   ```shell
   cratis observers retry-partition Core.MyFeature.MyReactor abc-123 -e MyStore
   ```

### Inspect a read model instance

```shell
cratis read-models list -e MyStore -o plain
cratis read-models get myReadModels abc-123 -e MyStore -o json
cratis read-models snapshots myReadModels abc-123 -e MyStore -o json
```

### Query events for a specific entity

```shell
cratis events has abc-123 -e MyStore -o plain
cratis events get -e MyStore --event-source-id abc-123 -o plain
cratis events get -e MyStore --event-type UserRegistered --from 0 --to 1000 -o plain
```

### Replay an observer

```shell
cratis observers replay Core.MyFeature.Listing.MyProjection -e MyStore
cratis observers show Core.MyFeature.Listing.MyProjection -e MyStore -o json
```

### Switch between environments

```shell
cratis context create dev --server "chronicle://localhost:35000/?disableTls=true" -e MyStore
cratis context create prod --server "chronicle://prod:35000" -e Production
cratis context set dev
cratis context list
```

---

## Key Things to Remember

- **Observer IDs are dotted type names**, not GUIDs: e.g. `Core.MyFeature.Listing.MyProjection`
- **Event type identifiers** use `Name` or `Name+Generation` format: e.g. `UserRegistered` or `UserRegistered+1`
- **Pages are 0-based** for `read-models instances`
- **`events count` returns the tail sequence number**, not a total count — gaps may exist
- **`login` and `logout` are top-level commands**, not nested under `auth`
- **Enums serialize as names** in JSON output (e.g. `"Client"`, `"Projection"`), not integers
- **`config path` output is format-independent** — same raw path regardless of `-o`
- **`llm-context` always outputs JSON** regardless of `-o`
