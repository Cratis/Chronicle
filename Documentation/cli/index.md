# Cratis CLI

The Cratis CLI (`cratis`) connects to a Chronicle server over gRPC to inspect and manage event stores, observers, projections, and read models.

## Key Capabilities

- **Inspect event stores** — list event stores, namespaces, event types, and query events
- **Manage observers** — list, show, replay, and retry reactors, reducers, and projections
- **Inspect read models** — list definitions, browse instances, view snapshots
- **Diagnose issues** — inspect failed partitions, view recommendations
- **Manage access** — authenticate users, manage OAuth applications
- **Multiple environments** — named contexts for switching between servers
- **AI-friendly output** — structured output formats designed for both human and machine consumption

## Quick Start

```shell
dotnet tool install --global Cratis.Chronicle.Cli
cratis config set server chronicle://localhost:35000/?disableTls=true
cratis event-stores list
```

## Topics

| Topic | Description |
| ----- | ----------- |
| [Installation](./installation.md) | Install and update the CLI |
| [Configuration](./configuration.md) | Connection strings, contexts, and settings |
| [Output Formats](./output-formats.md) | Controlling how commands display results |
| [Command Reference](./commands.md) | Complete list of all commands and options |
| [AI Agent Integration](./ai-integration.md) | Using the CLI as a tool for AI agents |
