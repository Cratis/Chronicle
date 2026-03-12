# AI Agent Integration

The Cratis CLI is designed to work as a tool for AI agents — coding assistants, autonomous agents, and LLM-based workflows that need to interact with a Chronicle event-sourcing system.

Every command supports structured output (`-o json`, `-o plain`, `-o json-compact`), each invocation is stateless, and `cratis llm-context` outputs the entire CLI surface as a single JSON document for use as a tool definition.

## Getting Started for AI Agents

### Step 1: Bootstrap with llm-context

Run `cratis llm-context` to get a machine-readable description of all commands, options, and examples:

```shell
cratis llm-context
```

This outputs a JSON document containing:

- All command groups and their commands
- Every option with its type and description
- Example invocations for each command
- Connection configuration details
- Per-command output format recommendations

Feed this into your agent's tool definitions to give it awareness of the full CLI surface.

### Step 2: Configure the connection

Set up the connection so subsequent commands don't need `--server`:

```shell
cratis config set server "chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000/?disableTls=true"
cratis config set event-store MyEventStore
```

### Step 3: Choose output formats

For AI agents, choose the output format based on what the agent needs to do:

| Scenario | Format | Why |
| -------- | ------ | --- |
| List things (event stores, observers, event types) | `plain` | 10-27x fewer tokens than JSON. Tab-separated, easy to parse. |
| Get structured details (config, read model instance) | `json` | Full structured data that agents can parse as key-value pairs. |
| Get structured details with token budget constraints | `json-compact` | Same data as `json` but ~30% fewer tokens (no whitespace). |
| Quick checks (does an event source have events?) | `plain` | Returns just `true` or `false`. |

General rule: **use `plain` unless you specifically need structured JSON data.** Plain output uses dramatically fewer tokens for most commands because JSON includes full schemas, causation chains, and nested metadata that inflate the response.

## Token Efficiency

The difference between output formats is significant for AI agents operating under token budgets:

| Command | Plain | JSON | Ratio |
| ------- | ----- | ---- | ----- |
| `event-types list` | ~1.2 KB | ~41 KB | 34x |
| `events get` (73 events) | ~6.8 KB | ~169 KB | 25x |
| `read-models list` | ~1.5 KB | ~40 KB | 27x |
| `event-stores list` | ~29 B | ~99 B | 3x |

The JSON output includes full JSON Schema blobs, causation chains with machine metadata, and deeply nested event context — valuable for detailed inspection but wasteful for overview queries.

## Common Agent Workflows

### Discover the system

```shell
cratis event-stores list -o plain
cratis namespaces list -e MyStore -o plain
cratis event-types list -e MyStore -o plain
cratis observers list -e MyStore -o plain
```

### Diagnose a failing observer

```shell
cratis observers list -e MyStore --type reactor -o plain
cratis observers show Core.MyFeature.MyReactor -e MyStore -o json
cratis failed-partitions list -e MyStore -o plain
cratis failed-partitions show Core.MyFeature.MyReactor abc-123 -e MyStore -o json
```

### Inspect read model state

```shell
cratis read-models list -e MyStore -o plain
cratis read-models instances myReadModels -e MyStore -o plain
cratis read-models get myReadModels abc-123 -e MyStore -o json
```

### Query events for an entity

```shell
cratis events has abc-123 -e MyStore -o plain
cratis events get -e MyStore --event-source-id abc-123 -o plain
cratis events get -e MyStore --event-type UserRegistered -o plain
```

### Trigger a replay

```shell
cratis observers replay Core.MyFeature.Listing.MyProjection -e MyStore
cratis observers show Core.MyFeature.Listing.MyProjection -e MyStore -o json
```

## Tool Definitions

The output of `cratis llm-context` is designed to feed directly into AI agent tool definition systems. Each command is described with:

- **Name** and **description** — for the agent to understand what the tool does
- **Options** with types — for the agent to construct valid invocations
- **Examples** — for few-shot learning in the agent's prompt
- **Output format guidance** — per-command recommendations the agent can follow

You can pipe `cratis llm-context` into your agent's system prompt or tool registration:

```shell
cratis llm-context > chronicle-tools.json
```

Then reference `chronicle-tools.json` as part of your agent's tool configuration.

## Non-Interactive Use

All commands work non-interactively except `cratis login` when called without `--password`. For fully automated pipelines:

```shell
cratis login admin --password "${CHRONICLE_PASSWORD}"
```

Or use client credentials in the connection string, which requires no login step:

```shell
export CHRONICLE_CONNECTION_STRING="chronicle://my-app:my-secret@chronicle-host:35000"
cratis event-stores list -o plain
```
