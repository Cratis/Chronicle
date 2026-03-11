# Cratis Chronicle CLI

The `cratis` CLI is a command-line tool for inspecting and managing a running Chronicle kernel. It connects over gRPC and provides commands for event stores, observers, projections, read models, and more.

## Installation

The CLI requires [.NET 10 SDK](https://dotnet.microsoft.com/download). Clone the repository and build from source:

```bash
git clone https://github.com/Cratis/Chronicle.git
cd Chronicle
dotnet build Source/Tools/Cli
```

Run directly with `dotnet run`:

```bash
dotnet run --project Source/Tools/Cli -- <command> [options]
```

Or build a self-contained binary and add it to your `PATH`:

```bash
dotnet publish Source/Tools/Cli -c Release -r linux-x64 --self-contained -o ~/.local/bin/cratis
# On macOS
dotnet publish Source/Tools/Cli -c Release -r osx-x64 --self-contained -o /usr/local/bin/cratis
```

## Connecting to a Server

By default the CLI connects to `chronicle://localhost:35000` (the standard development port). The connection string is resolved in the following order:

1. `--server <CONNECTION_STRING>` flag on any command
2. `CHRONICLE_CONNECTION_STRING` environment variable
3. `defaultServer` in the [config file](#configuration)
4. `chronicle://localhost:35000` (built-in default)

```bash
# Explicit flag
cratis event-stores list --server chronicle://myhost:35000

# Environment variable
export CHRONICLE_CONNECTION_STRING=chronicle://myhost:35000
cratis event-stores list

# Persist via config
cratis config set server chronicle://myhost:35000
```

## Global Options

These options are available on every command:

| Option | Description | Default |
|--------|-------------|---------|
| `--server <CONNECTION_STRING>` | Chronicle server connection string | See above |
| `-o, --output <FORMAT>` | Output format: `json`, `text`, or `plain` | `auto` |

**Output format auto-detection:**
- `text` — rich tables rendered with Spectre.Console (interactive terminal)
- `plain` — plain text tables (when `NO_COLOR` environment variable is set)
- `json` — JSON output (when stdout is redirected / piped)

## Configuration

The CLI stores persistent defaults at `~/.config/cratis/config.json` (respects `XDG_CONFIG_HOME`).

| Key | Description |
|-----|-------------|
| `server` | Default Chronicle server connection string |
| `event-store` | Default event store name |
| `namespace` | Default namespace name |

```bash
# Set defaults so you don't have to repeat flags
cratis config set server chronicle://myhost:35000
cratis config set event-store my-app
cratis config set namespace production

# Show current configuration
cratis config show

# Print the path to the config file
cratis config path
```

## Commands

### `event-stores`

```bash
cratis event-stores list
```

Lists all event stores registered with the Chronicle kernel.

---

### `namespaces`

```bash
cratis namespaces list [options]
```

Lists namespaces within an event store.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |

---

### `event-types`

```bash
cratis event-types list [options]
```

Lists all registered event types.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |

---

### `events`

#### `events get`

```bash
cratis events get [options]
```

Retrieves events from an event sequence.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |
| `--sequence <ID>` | Event sequence ID | `event-log` |
| `--from <NUMBER>` | Starting sequence number | `0` |
| `--to <NUMBER>` | Ending sequence number (inclusive) | _(end)_ |
| `--event-source-id <ID>` | Filter by event source ID | _(all)_ |

**Examples:**

```bash
# Get all events from the default event log
cratis events get

# Get events 100–200 from a specific event store
cratis events get -e my-app --from 100 --to 200

# Get all events for a specific aggregate instance
cratis events get --event-source-id order-42
```

#### `events count`

```bash
cratis events count [options]
```

Returns the tail sequence number (total event count) of an event sequence.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |
| `--sequence <ID>` | Event sequence ID | `event-log` |

---

### `observers`

Observers include reactors, reducers, and projections.

#### `observers list`

```bash
cratis observers list [options]
```

Lists all observers registered in an event store/namespace.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |

#### `observers replay`

```bash
cratis observers replay <OBSERVER_ID> [options]
```

Replays an observer from the beginning of its event sequence.

| Argument / Option | Description | Default |
|-------------------|-------------|---------|
| `<OBSERVER_ID>` | The observer ID _(required)_ | |
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |
| `--sequence <ID>` | Event sequence ID | `event-log` |

#### `observers replay-partition`

```bash
cratis observers replay-partition <OBSERVER_ID> <PARTITION> [options]
```

Replays a single partition of an observer from its beginning.

| Argument / Option | Description | Default |
|-------------------|-------------|---------|
| `<OBSERVER_ID>` | The observer ID _(required)_ | |
| `<PARTITION>` | The partition key _(required)_ | |
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |
| `--sequence <ID>` | Event sequence ID | `event-log` |

#### `observers retry-partition`

```bash
cratis observers retry-partition <OBSERVER_ID> <PARTITION> [options]
```

Retries a failed partition of an observer without full replay.

Same arguments and options as `replay-partition`.

---

### `failed-partitions`

```bash
cratis failed-partitions list [options]
```

Lists observer partitions that are currently in a failed state.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |

---

### `recommendations`

The Chronicle kernel may issue recommendations (e.g. "replay this projection after a schema change"). These commands let you inspect and act on them.

#### `recommendations list`

```bash
cratis recommendations list [options]
```

Lists pending recommendations.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |

#### `recommendations perform`

```bash
cratis recommendations perform <RECOMMENDATION_ID> [options]
```

Carries out a recommendation.

| Argument | Description |
|----------|-------------|
| `<RECOMMENDATION_ID>` | The recommendation GUID _(required)_ |

#### `recommendations ignore`

```bash
cratis recommendations ignore <RECOMMENDATION_ID> [options]
```

Dismisses a recommendation without acting on it.

---

### `identities`

```bash
cratis identities list [options]
```

Lists identities known to the Chronicle kernel.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |

---

### `projections`

#### `projections list`

```bash
cratis projections list [options]
```

Lists all projection definitions.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |

#### `projections show`

```bash
cratis projections show <PROJECTION_ID> [options]
```

Shows the full projection declaration for a specific projection.

---

### `read-models`

#### `read-models list`

```bash
cratis read-models list [options]
```

Lists registered read model definitions.

| Option | Description | Default |
|--------|-------------|---------|
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |

#### `read-models instances`

```bash
cratis read-models instances <READ_MODEL> [options]
```

Paginates the stored instances of a read model.

| Argument / Option | Description | Default |
|-------------------|-------------|---------|
| `<READ_MODEL>` | Read model container name _(required)_ | |
| `-e, --event-store <NAME>` | Event store name | `default` |
| `-n, --namespace <NAME>` | Namespace | `default` |
| `--page <NUMBER>` | Page number (1-based) | `1` |
| `--page-size <SIZE>` | Instances per page | `20` |

---

## Scripting / CI Usage

When stdout is redirected the CLI automatically switches to JSON output, making it easy to process results with tools like `jq`:

```bash
# Count events and parse with jq
cratis events count -e my-app | jq '.tailSequenceNumber'

# List all failed partitions as JSON
cratis failed-partitions list -e my-app -n production | jq '.[]'

# Suppress colour explicitly
cratis observers list -o plain
```

Set `CHRONICLE_CONNECTION_STRING` in your CI environment rather than passing `--server` on every command.

---

For a machine-readable description of all CLI capabilities (commands, options, connection info), use the `llm-context` command:

```bash
cratis llm-context
```

This outputs a JSON schema of the entire CLI surface — ideal for tool-use integration with AI agents.

---

### `llm-context`

```bash
cratis llm-context
```

Outputs a machine-readable JSON description of all CLI commands, options, and connection information. Designed for AI agents to discover CLI capabilities programmatically.
