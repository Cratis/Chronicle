# Command Reference

All commands support the global `-o` / `--output` flag (see [Output Formats](./output-formats.md)). Commands that operate within an event store accept `-e` / `--event-store` and `-n` / `--namespace` flags.

## Global Options

| Option | Type | Default | Description |
| ------ | ---- | ------- | ----------- |
| `--server` | string | — | Chronicle server connection string |
| `-o`, `--output` | string | `auto` | Output format: `json`, `text`, `plain`, or `json-compact` |

## Event Store Options

Most commands that operate within an event store accept:

| Option | Type | Default | Description |
| ------ | ---- | ------- | ----------- |
| `-e`, `--event-store` | string | `default` | Event store name |
| `-n`, `--namespace` | string | `default` | Namespace within the event store |

## event-stores

| Command | Description |
| ------- | ----------- |
| `cratis event-stores list` | List all event stores on the server |

No additional options.

## namespaces

| Command | Description |
| ------- | ----------- |
| `cratis namespaces list` | List namespaces within an event store |

Accepts [event store options](#event-store-options).

## event-types

| Command | Description |
| ------- | ----------- |
| `cratis event-types list` | List all registered event types |
| `cratis event-types show <EVENT_TYPE>` | Show an event type registration including its JSON schema |

Accepts [event store options](#event-store-options).

### event-types show options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<EVENT_TYPE>` | string | Event type identifier: `Name` or `Name+Generation` (e.g. `UserRegistered+1`) |

## events

| Command | Description |
| ------- | ----------- |
| `cratis events get` | Get events from an event sequence |
| `cratis events count` | Get the highest used sequence number (tail). Not a total count — gaps may exist. |
| `cratis events has <EVENT_SOURCE_ID>` | Check if events exist for a given event source ID |

Accepts [event store options](#event-store-options).

### events get options

| Option | Type | Default | Description |
| ------ | ---- | ------- | ----------- |
| `--sequence` | string | `event-log` | Event sequence name |
| `--from` | ulong | — | Start sequence number |
| `--to` | ulong | — | End sequence number |
| `--event-source-id` | string | — | Filter by event source identifier |
| `--event-type` | string | — | Filter by event type (e.g. `UserRegistered` or `UserRegistered+1`). Comma-separate multiple. |

### events count options

| Option | Type | Default | Description |
| ------ | ---- | ------- | ----------- |
| `--sequence` | string | `event-log` | Event sequence name |

### events has options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<EVENT_SOURCE_ID>` | string | Event source identifier |
| `--sequence` | string | Event sequence name (default: `event-log`) |

## observers

Observers include reactors, reducers, and projections.

| Command | Description |
| ------- | ----------- |
| `cratis observers list` | List observers |
| `cratis observers show <OBSERVER_ID>` | Show detailed observer information |
| `cratis observers replay <OBSERVER_ID>` | Replay an observer from the beginning |
| `cratis observers replay-partition <OBSERVER_ID> <PARTITION>` | Replay a specific partition |
| `cratis observers retry-partition <OBSERVER_ID> <PARTITION>` | Retry a failed partition |

Accepts [event store options](#event-store-options).

### observers list options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `-t`, `--type` | string | Filter by type: `reactor`, `reducer`, `projection`, or `all`. Invalid values return an error. |

### observers show / replay options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<OBSERVER_ID>` | string | Observer identifier from `observers list`. Dotted type name (e.g. `Core.MyFeature.Listing.MyProjection`). |
| `--sequence` | string | Event sequence name (default: `event-log`) |

### observers replay-partition / retry-partition options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<OBSERVER_ID>` | string | Observer identifier from `observers list` |
| `<PARTITION>` | string | Partition key (typically an event source ID) |
| `--sequence` | string | Event sequence name (default: `event-log`) |

## failed-partitions

| Command | Description |
| ------- | ----------- |
| `cratis failed-partitions list` | List failed partitions |
| `cratis failed-partitions show <OBSERVER_ID> <PARTITION>` | Show error details and stack traces for a failed partition |

Accepts [event store options](#event-store-options).

### failed-partitions list options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `--observer` | string | Filter by observer identifier |

### failed-partitions show options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<OBSERVER_ID>` | string | Observer identifier from `observers list` |
| `<PARTITION>` | string | Partition key from `failed-partitions list` |

## projections

| Command | Description |
| ------- | ----------- |
| `cratis projections list` | List projection definitions |
| `cratis projections show <IDENTIFIER>` | Show a projection declaration |

Accepts [event store options](#event-store-options).

### projections show options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<IDENTIFIER>` | string | Projection identifier |

## read-models

| Command | Description |
| ------- | ----------- |
| `cratis read-models list` | List read model definitions |
| `cratis read-models instances <CONTAINER>` | List instances in a read model container |
| `cratis read-models get <CONTAINER> <KEY>` | Get a single instance by key |
| `cratis read-models occurrences <TYPE>` | List replay history for a read model type |
| `cratis read-models snapshots <CONTAINER> <KEY>` | Get snapshots for a specific instance |

Accepts [event store options](#event-store-options).

### read-models instances options

| Option | Type | Default | Description |
| ------ | ---- | ------- | ----------- |
| `<CONTAINER>` | string | — | Read model container name |
| `--page` | int | `0` | Page number (0-based) |
| `--page-size` | int | `20` | Items per page |

### read-models get options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<CONTAINER>` | string | Read model container name from `read-models list` |
| `<KEY>` | string | Read model instance key (typically an event source ID) |
| `--sequence` | string | Event sequence name (default: `event-log`) |

### read-models occurrences options

| Option | Type | Default | Description |
| ------ | ---- | ------- | ----------- |
| `<READ_MODEL_TYPE>` | string | — | Read model type identifier from `read-models list` |
| `--generation` | uint | `1` | Read model type generation |

### read-models snapshots options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<CONTAINER>` | string | Read model container name from `read-models list` |
| `<KEY>` | string | Read model instance key (typically an event source ID) |
| `--sequence` | string | Event sequence name (default: `event-log`) |

## recommendations

| Command | Description |
| ------- | ----------- |
| `cratis recommendations list` | List system recommendations |
| `cratis recommendations perform <ID>` | Perform a recommendation |
| `cratis recommendations ignore <ID>` | Ignore a recommendation |

Accepts [event store options](#event-store-options).

### recommendations perform / ignore options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<ID>` | guid | Recommendation identifier |

## identities

| Command | Description |
| ------- | ----------- |
| `cratis identities list` | List known identities |

Accepts [event store options](#event-store-options). No additional options.

## auth

| Command | Description |
| ------- | ----------- |
| `cratis login <USERNAME>` | Log in via the password grant flow |
| `cratis logout` | Clear the cached login session |
| `cratis auth status` | Show current authentication status |

`login` and `logout` are top-level commands, not nested under `auth`.

### login options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<USERNAME>` | string | The username to log in with |
| `--password` | string | Password for non-interactive login. If omitted, prompts interactively. |

## users

| Command | Description |
| ------- | ----------- |
| `cratis users list` | List all users |
| `cratis users add <USERNAME> <EMAIL> <PASSWORD>` | Add a new user |
| `cratis users remove <USER_ID>` | Remove a user |

Accepts [event store options](#event-store-options).

### users add options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<USERNAME>` | string | The username for the new user |
| `<EMAIL>` | string | The email address for the new user |
| `<PASSWORD>` | string | The initial password for the new user |

### users remove options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<USER_ID>` | guid | The unique identifier of the user to remove |

## applications

| Command | Description |
| ------- | ----------- |
| `cratis applications list` | List OAuth client applications |
| `cratis applications add <CLIENT_ID> <CLIENT_SECRET>` | Add a new application |
| `cratis applications remove <APP_ID>` | Remove an application |
| `cratis applications rotate-secret <APP_ID> <NEW_SECRET>` | Rotate a client secret |

Accepts [event store options](#event-store-options).

### applications add options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<CLIENT_ID>` | string | The client identifier for the new application |
| `<CLIENT_SECRET>` | string | The client secret for the new application |

### applications remove options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<APP_ID>` | guid | The unique identifier of the application to remove |

### applications rotate-secret options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<APP_ID>` | guid | The unique identifier of the application |
| `<NEW_SECRET>` | string | The new client secret |

## context

| Command | Description |
| ------- | ----------- |
| `cratis context list` | List all named contexts |
| `cratis context create <NAME>` | Create a new context |
| `cratis context set <NAME>` | Switch to a context |
| `cratis context show` | Show the active context |
| `cratis context delete <NAME>` | Delete a context (cannot delete the active context) |
| `cratis context rename <OLD> <NEW>` | Rename a context |

### context create options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<NAME>` | string | Name of the context to create |
| `--server` | string | Chronicle server connection string for this context |
| `-e`, `--event-store` | string | Default event store for this context |
| `-n`, `--namespace` | string | Default namespace for this context |

### context set / delete options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<NAME>` | string | Name of the context |

### context rename options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<OLD>` | string | Current context name |
| `<NEW>` | string | New context name |

## config

| Command | Description |
| ------- | ----------- |
| `cratis config show` | Show current configuration |
| `cratis config set <KEY> <VALUE>` | Set a configuration value |
| `cratis config path` | Print the configuration file path |

### config set options

| Option | Type | Description |
| ------ | ---- | ----------- |
| `<KEY>` | string | Key to set: `server`, `event-store`, `namespace`, `client-id`, or `client-secret` |
| `<VALUE>` | string | Value to assign |

## llm-context

| Command | Description |
| ------- | ----------- |
| `cratis llm-context` | Output full CLI capabilities as JSON for AI agent consumption. Always outputs JSON regardless of `-o` flag. |
