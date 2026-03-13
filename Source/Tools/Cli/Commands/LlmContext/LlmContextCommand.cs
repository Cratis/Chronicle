// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.LlmContext;

/// <summary>
/// Outputs a machine-readable description of all CLI capabilities for AI agents.
/// </summary>
public class LlmContextCommand : AsyncCommand<GlobalSettings>
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var descriptor = new LlmContextDescriptor
        {
            Tool = "cratis",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0",
            Description = "CLI for managing Chronicle event-sourced systems. Connects to a Chronicle server over gRPC.",
            GlobalOptions =
            [
                new OptionDescriptor("--server", "string", "Chronicle server connection string (e.g. chronicle://localhost:35000)"),
                new OptionDescriptor("-o, --output", "string", "Output format: json, text, plain, or json-compact. Defaults to auto-detection. Use 'json-compact' for compact non-indented JSON (fewer tokens than 'json', same content)."),
            ],
            CommandGroups = BuildCommandGroups(),
            ConnectionInfo = new ConnectionInfoDescriptor
            {
                DefaultConnectionString = "chronicle://<client>:<secret>@localhost:35000",
                EnvironmentVariable = CliDefaults.ConnectionStringEnvVar,
                ConfigFile = CliConfiguration.GetConfigPath(),
                Precedence = ["--server flag", "CHRONICLE_CONNECTION_STRING env var", "config file", "default (localhost:35000)"],
            },
            Tips =
            [
                "Use '-o json-compact' for compact (non-indented) JSON — fewer tokens than '-o json' while preserving full structured detail. Best when you need structured data but want to avoid whitespace overhead.",
                "Prefer --output plain for AI consumption — it uses dramatically fewer tokens than JSON for most commands (up to 27x fewer).",
                "JSON output includes raw schemas, causation chains, and nested metadata that inflate token usage. Only use --output json (or json-compact to save whitespace tokens) when you need to parse structured key-value data (e.g. config show, projections show).",
                "For listing commands (event-stores, namespaces, event-types, observers, read-models, projections, failed-partitions, recommendations, identities), always use --output plain.",
                "Enums in JSON output serialize as human-readable names (e.g. 'Client', 'Projection') rather than integers.",
                "Pipe plain output through grep/awk for filtering; use --output json with jq only when structured parsing is essential.",
                "Set a default server with: cratis config set server chronicle://myhost:35000",
                "Most commands require --event-store and --namespace; both default to 'default'.",
                "Use 'cratis observers list --type reactor' to filter by observer type.",
                "config path outputs the same format regardless of --output flag.",
            ],
            OutputFormatGuidance = new OutputFormatGuidanceDescriptor
            {
                Summary = "Use --output plain for nearly all commands — it is the most token-efficient format, often 10-27x smaller than JSON. Use --output json-compact when you need structured data but want to avoid whitespace overhead (same content as json, no indentation). JSON and json-compact include raw schemas, causation chains, and deeply nested metadata — only use them when you need to parse structured values (e.g. config show, read-models get, llm-context).",
                PerCommand =
                [
                    new CommandOutputAdvice("event-stores list", "plain", "plain is ~3x smaller (29B vs 99B). JSON wraps each name in {\"value\": ...}."),
                    new CommandOutputAdvice("namespaces list", "plain", "Nearly identical size. Plain avoids JSON array brackets."),
                    new CommandOutputAdvice("event-types list", "plain", "plain is ~34x smaller (1.2KB vs 41KB). JSON includes full JSON Schema blob per event type."),
                    new CommandOutputAdvice("events get", "plain", "plain is ~25x smaller (6.8KB vs 169KB for 73 events). JSON includes context, causation chains, content."),
                    new CommandOutputAdvice("events tail", "plain", "plain returns just the number (3B vs 31B)."),
                    new CommandOutputAdvice("observers list", "plain", "When empty, JSON is smaller (2B vs 44B), but with data plain is comparable. Use plain for consistency."),
                    new CommandOutputAdvice("projections list", "plain", "JSON output is very large due to full projection definitions with all From/Join/RemovedWith mappings. Plain shows the key fields concisely."),
                    new CommandOutputAdvice("projections show", "json", "JSON (612B) and plain (574B) are similar size. JSON is easier to parse for the declaration field."),
                    new CommandOutputAdvice("read-models list", "plain", "plain is ~27x smaller (1.5KB vs 40KB). JSON includes full schema blobs per read model."),
                    new CommandOutputAdvice("read-models instances", "plain", "Both formats are comparable; use plain for consistency."),
                    new CommandOutputAdvice("read-models get", "json", "JSON contains the full read model document. Use JSON for structured parsing."),
                    new CommandOutputAdvice("read-models occurrences", "plain", "Use plain for consistency with other listing commands."),
                    new CommandOutputAdvice("read-models snapshots", "json", "JSON contains full snapshot documents with event details. Use JSON for structured parsing."),
                    new CommandOutputAdvice("observers show", "json", "JSON contains all observer fields. Use JSON for structured parsing, plain for quick overview."),
                    new CommandOutputAdvice("event-types show", "json", "JSON contains the full JSON schema. Use JSON to parse schema fields."),
                    new CommandOutputAdvice("events has", "plain", "Plain returns just 'true' or 'false'. Most token-efficient for boolean checks."),
                    new CommandOutputAdvice("failed-partitions show", "json", "JSON contains full error messages and stack traces. Use JSON for structured error analysis."),
                    new CommandOutputAdvice("failed-partitions list", "plain", "When empty, JSON is smaller (2B vs 33B). With data, use plain for consistency."),
                    new CommandOutputAdvice("identities list", "plain", "Use plain for consistency with other listing commands."),
                    new CommandOutputAdvice("recommendations list", "plain", "When empty, JSON is smaller (2B vs 34B). With data, use plain for consistency."),
                    new CommandOutputAdvice("config show", "json", "JSON is slightly smaller (85B vs 117B) and structured for key-value parsing."),
                    new CommandOutputAdvice("config path", "plain", "Both formats output identical raw path text."),
                    new CommandOutputAdvice("login", "plain", "Top-level command (not 'auth login'). Prompts for password interactively, or use --password for non-interactive auth."),
                    new CommandOutputAdvice("logout", "plain", "Top-level command (not 'auth logout'). Plain outputs a simple confirmation message."),
                    new CommandOutputAdvice("auth status", "json", "JSON is structured for key-value parsing. Use JSON when checking auth state programmatically."),
                    new CommandOutputAdvice("context list", "plain", "Use plain for consistency with other listing commands."),
                    new CommandOutputAdvice("context show", "json", "JSON is structured for key-value parsing."),
                    new CommandOutputAdvice("context create", "plain", "Plain outputs a confirmation message."),
                    new CommandOutputAdvice("context set", "plain", "Plain outputs a confirmation message."),
                    new CommandOutputAdvice("context delete", "plain", "Plain outputs a confirmation message."),
                    new CommandOutputAdvice("context rename", "plain", "Plain outputs a confirmation message."),
                    new CommandOutputAdvice("users list", "plain", "Use plain for consistency with other listing commands."),
                    new CommandOutputAdvice("users add", "plain", "Plain outputs a simple confirmation message."),
                    new CommandOutputAdvice("users remove", "plain", "Plain outputs a simple confirmation message."),
                    new CommandOutputAdvice("applications list", "plain", "Use plain for consistency with other listing commands."),
                    new CommandOutputAdvice("applications add", "plain", "Plain outputs a simple confirmation message."),
                    new CommandOutputAdvice("applications remove", "plain", "Plain outputs a simple confirmation message."),
                    new CommandOutputAdvice("applications rotate-secret", "plain", "Plain outputs a simple confirmation message."),
                    new CommandOutputAdvice("llm-context", "json", "Always outputs JSON regardless of --output flag."),
                ],
            },
        };

        Console.WriteLine(JsonSerializer.Serialize(descriptor, _serializerOptions));
        return Task.FromResult(ExitCodes.Success);
    }

    static IReadOnlyList<CommandGroupDescriptor> BuildCommandGroups() =>
    [
        new(
            "event-stores",
            "Manage event stores",
            [new CommandDescriptor("list", "List all event stores", null, null, ["cratis event-stores list", "cratis event-stores list -o plain"])]),
        new(
            "namespaces",
            "Manage namespaces within an event store",
            [new CommandDescriptor("list", "List namespaces in an event store", EventStoreOptions(), null, ["cratis namespaces list", "cratis namespaces list -e MyStore"])]),
        new(
            "event-types",
            "Manage event types",
            [
                new CommandDescriptor("list", "List registered event types", EventStoreOptions(), null, ["cratis event-types list", "cratis event-types list -e MyStore -o plain"]),
                new CommandDescriptor(
                    "show",
                    "Show an event type registration with its JSON schema",
                    EventStoreOptions(),
                    [new OptionDescriptor("<EVENT_TYPE>", "string", "Event type identifier: name or name+generation (positional)")],
                    ["cratis event-types show UserRegistered", "cratis event-types show UserRegistered+1 -o json"]),
            ]),
        new(
            "events",
            "Query and inspect events",
            [
                new CommandDescriptor(
                    "get",
                    "Get events from an event sequence",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                        new OptionDescriptor("--from", "ulong", "Start sequence number"),
                        new OptionDescriptor("--to", "ulong", "End sequence number"),
                        new OptionDescriptor("--event-source-id", "string", "Filter by event source identifier"),
                        new OptionDescriptor("--event-type", "string", "Filter by event type (e.g. UserRegistered or UserRegistered+1). Comma-separate multiple."),
                    ],
                    ["cratis events get -o plain", "cratis events get --from 100 --to 200", "cratis events get --event-source-id abc-123", "cratis events get --event-type UserRegistered"]),
                new CommandDescriptor(
                    "count",
                    "Get the tail (highest) sequence number. Note: not a count of events — gaps may exist.",
                    EventStoreOptions(),
                    [new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)")],
                    ["cratis events tail", "cratis events tail -e MyStore"]),
                new CommandDescriptor(
                    "has",
                    "Check if events exist for an event source ID",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<EVENT_SOURCE_ID>", "string", "Event source identifier (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ],
                    ["cratis events has abc-123", "cratis events has abc-123 -o plain"]),
            ]),
        new(
            "observers",
            "Manage observers (reactors, reducers, projections)",
            [
                new CommandDescriptor(
                    "list",
                    "List observers",
                    EventStoreOptions(),
                    [new OptionDescriptor("-t, --type", "string", "Filter by type: reactor, reducer, projection, or all. Invalid values return an error.")],
                    ["cratis observers list", "cratis observers list --type reactor -o plain"]),
                new CommandDescriptor(
                    "show",
                    "Show detailed information about a specific observer",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional). Format varies: projections use dotted type names (e.g. Core.MyFeature.Listing.MyProjection), system observers use prefixed names."),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ],
                    ["cratis observers show Core.MyFeature.Listing.MyProjection"]),
                new CommandDescriptor(
                    "replay",
                    "Replay an observer from the beginning. Use 'cratis observers show <id>' to check progress.",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ],
                    ["cratis observers replay Core.MyFeature.Listing.MyProjection"]),
                new CommandDescriptor(
                    "replay-partition",
                    "Replay a specific partition of an observer",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)"),
                        new OptionDescriptor("<PARTITION>", "string", "Partition key (typically an event source ID) (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ],
                    ["cratis observers replay-partition Core.MyFeature.Listing.MyProjection abc-123"]),
                new CommandDescriptor(
                    "retry-partition",
                    "Retry a failed partition. Use 'cratis observers show <id>' to check progress.",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)"),
                        new OptionDescriptor("<PARTITION>", "string", "Partition key (typically an event source ID, from 'cratis failed-partitions list') (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ],
                    ["cratis observers retry-partition Core.MyFeature.Listing.MyProjection abc-123"]),
            ]),
        new(
            "failed-partitions",
            "Inspect failed observer partitions",
            [
                new CommandDescriptor(
                    "list",
                    "List failed partitions",
                    EventStoreOptions(),
                    [new OptionDescriptor("--observer", "string", "Filter by observer identifier")],
                    ["cratis failed-partitions list", "cratis failed-partitions list --observer 550e8400-e29b-41d4-a716-446655440000"]),
                new CommandDescriptor(
                    "show",
                    "Show detailed information about a specific failed partition with error messages and stack traces",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)"),
                        new OptionDescriptor("<PARTITION>", "string", "Partition key (typically an event source ID, from 'cratis failed-partitions list') (positional)"),
                    ],
                    ["cratis failed-partitions show 550e8400-e29b-41d4-a716-446655440000 my-partition"]),
            ]),
        new(
            "recommendations",
            "Manage system recommendations",
            [
                new CommandDescriptor("list", "List recommendations", EventStoreOptions(), null, ["cratis recommendations list"]),
                new CommandDescriptor(
                    "perform",
                    "Perform a recommendation",
                    EventStoreOptions(),
                    [new OptionDescriptor("<RECOMMENDATION_ID>", "guid", "Recommendation identifier (positional)")],
                    ["cratis recommendations perform 550e8400-e29b-41d4-a716-446655440000"]),
                new CommandDescriptor(
                    "ignore",
                    "Ignore a recommendation",
                    EventStoreOptions(),
                    [new OptionDescriptor("<RECOMMENDATION_ID>", "guid", "Recommendation identifier (positional)")],
                    ["cratis recommendations ignore 550e8400-e29b-41d4-a716-446655440000"]),
            ]),
        new(
            "identities",
            "Inspect identities",
            [new CommandDescriptor("list", "List known identities", EventStoreOptions(), null, ["cratis identities list -o plain"])]),
        new(
            "projections",
            "Manage projections",
            [
                new CommandDescriptor("list", "List projection definitions", EventStoreOptions(), null, ["cratis projections list", "cratis projections list -o plain"]),
                new CommandDescriptor(
                    "show",
                    "Show a projection declaration",
                    EventStoreOptions(),
                    [new OptionDescriptor("<IDENTIFIER>", "string", "Projection identifier (positional)")],
                    ["cratis projections show MyProjection -o json"]),
            ]),
        new(
            "read-models",
            "Inspect read model data",
            [
                new CommandDescriptor("list", "List read model definitions", EventStoreOptions(), null, ["cratis read-models list", "cratis read-models list -o plain"]),
                new CommandDescriptor(
                    "instances",
                    "List read model instances",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<READ_MODEL>", "string", "Read model container name (positional)"),
                        new OptionDescriptor("--page", "int", "Page number, 0-based (default: 0)"),
                        new OptionDescriptor("--page-size", "int", "Items per page (default: 20)"),
                    ],
                    ["cratis read-models instances MyReadModel", "cratis read-models instances MyReadModel --page 1 --page-size 50"]),
                new CommandDescriptor(
                    "get",
                    "Get a single read model instance by key",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<READ_MODEL>", "string", "Read model container name (from 'cratis read-models list') (positional)"),
                        new OptionDescriptor("<KEY>", "string", "Read model instance key (typically an event source ID) (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ],
                    ["cratis read-models get MyReadModel abc-123", "cratis read-models get MyReadModel abc-123 -o json"]),
                new CommandDescriptor(
                    "occurrences",
                    "List read model occurrences (replay history)",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<READ_MODEL_TYPE>", "string", "Read model type identifier (from 'cratis read-models list') (positional)"),
                        new OptionDescriptor("--generation", "uint", "Read model type generation (default: 1)"),
                    ],
                    ["cratis read-models occurrences MyReadModelType"]),
                new CommandDescriptor(
                    "snapshots",
                    "Get snapshots for a read model instance by key",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<READ_MODEL>", "string", "Read model container name (from 'cratis read-models list') (positional)"),
                        new OptionDescriptor("<KEY>", "string", "Read model instance key (typically an event source ID) (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ],
                    ["cratis read-models snapshots MyReadModel abc-123"]),
            ]),
        new(
            "auth",
            "Authentication status",
            [
                new CommandDescriptor("status", "Show current authentication status (login session, client credentials, server)", null, null, ["cratis auth status", "cratis auth status -o json"]),
            ]),
        new(
            "(top-level)",
            "Top-level authentication commands (not nested under a group)",
            [
                new CommandDescriptor(
                    "login",
                    "Log in as a user via the password grant flow. Prompts for password interactively unless --password is provided. Usage: cratis login <USERNAME> [--password <PASSWORD>]",
                    null,
                    [
                        new OptionDescriptor("<USERNAME>", "string", "The username to log in with (positional)"),
                        new OptionDescriptor("--password", "string", "Password for non-interactive login. If omitted, prompts interactively."),
                    ],
                    ["cratis login admin", "cratis login admin --password P@ssw0rd"]),
                new CommandDescriptor("logout", "Clear the cached login session. Usage: cratis logout", null, null, ["cratis logout"]),
            ]),
        new(
            "users",
            "Manage Chronicle users",
            [
                new CommandDescriptor("list", "List all users", EventStoreOptions(), null, ["cratis users list", "cratis users list -o plain"]),
                new CommandDescriptor(
                    "add",
                    "Add a new user",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<USERNAME>", "string", "The username for the new user (positional)"),
                        new OptionDescriptor("<EMAIL>", "string", "The email address for the new user (positional)"),
                        new OptionDescriptor("<PASSWORD>", "string", "The initial password for the new user (positional)"),
                    ],
                    ["cratis users add alice alice@example.com P@ssw0rd!"]),
                new CommandDescriptor(
                    "remove",
                    "Remove a user",
                    EventStoreOptions(),
                    [new OptionDescriptor("<USER_ID>", "guid", "The unique identifier of the user to remove (positional)")],
                    ["cratis users remove 550e8400-e29b-41d4-a716-446655440000"]),
            ]),
        new(
            "applications",
            "Manage OAuth client applications",
            [
                new CommandDescriptor("list", "List all applications", EventStoreOptions(), null, ["cratis applications list", "cratis applications list -o plain"]),
                new CommandDescriptor(
                    "add",
                    "Add a new application (OAuth client)",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<CLIENT_ID>", "string", "The client identifier for the new application (positional)"),
                        new OptionDescriptor("<CLIENT_SECRET>", "string", "The client secret for the new application (positional)"),
                    ],
                    ["cratis applications add my-app my-secret"]),
                new CommandDescriptor(
                    "remove",
                    "Remove an application",
                    EventStoreOptions(),
                    [new OptionDescriptor("<APP_ID>", "guid", "The unique identifier of the application to remove (positional)")],
                    ["cratis applications remove 550e8400-e29b-41d4-a716-446655440000"]),
                new CommandDescriptor(
                    "rotate-secret",
                    "Rotate an application's client secret",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<APP_ID>", "guid", "The unique identifier of the application (positional)"),
                        new OptionDescriptor("<NEW_SECRET>", "string", "The new client secret (positional)"),
                    ],
                    ["cratis applications rotate-secret 550e8400-e29b-41d4-a716-446655440000 new-secret"]),
            ]),
        new(
            "context",
            "Manage named connection contexts (server, event store, namespace per environment)",
            [
                new CommandDescriptor("list", "List all contexts", null, null, ["cratis context list"]),
                new CommandDescriptor(
                    "create",
                    "Create a new named context. If it is the first context, it becomes active automatically.",
                    null,
                    [
                        new OptionDescriptor("<NAME>", "string", "Name of the context to create (positional)"),
                        new OptionDescriptor("--server", "string", "Chronicle server connection string for this context"),
                        new OptionDescriptor("-e, --event-store", "string", "Default event store for this context"),
                        new OptionDescriptor("-n, --namespace", "string", "Default namespace for this context"),
                    ],
                    ["cratis context create dev --server chronicle://localhost:35000/?disableTls=true", "cratis context create prod --server chronicle://prod:35000 -e production"]),
                new CommandDescriptor(
                    "set",
                    "Switch to a named context",
                    null,
                    [new OptionDescriptor("<NAME>", "string", "Name of the context to switch to (positional)")],
                    ["cratis context set prod"]),
                new CommandDescriptor("show", "Show current context details", null, null, ["cratis context show", "cratis context show -o json"]),
                new CommandDescriptor(
                    "delete",
                    "Delete a named context. Cannot delete the active context.",
                    null,
                    [new OptionDescriptor("<NAME>", "string", "Name of the context to delete (positional)")],
                    ["cratis context delete staging"]),
                new CommandDescriptor(
                    "rename",
                    "Rename a context",
                    null,
                    [
                        new OptionDescriptor("<OLD_NAME>", "string", "Current context name (positional)"),
                        new OptionDescriptor("<NEW_NAME>", "string", "New context name (positional)"),
                    ],
                    ["cratis context rename dev development"]),
            ]),
        new(
            "config",
            "Manage CLI configuration",
            [
                new CommandDescriptor("show", "Show current configuration", null, null, ["cratis config show", "cratis config show -o json"]),
                new CommandDescriptor(
                    "set",
                    "Set a configuration value",
                    null,
                    [
                        new OptionDescriptor("<KEY>", "string", "Key to set: server, event-store, namespace, client-id, or client-secret (positional). These update the active context's defaults."),
                        new OptionDescriptor("<VALUE>", "string", "Value to assign (positional)"),
                    ],
                    ["cratis config set server chronicle://myhost:35000", "cratis config set event-store MyStore", "cratis config set client-id my-app", "cratis config set client-secret my-secret"]),
                new CommandDescriptor("path", "Print configuration file path", null, null, ["cratis config path"]),
            ]),
    ];

    static IReadOnlyList<OptionDescriptor> EventStoreOptions() =>
    [
        new("-e, --event-store", "string", "Event store name (default: default)"),
        new("-n, --namespace", "string", "Namespace within the event store (default: default)"),
    ];
}
