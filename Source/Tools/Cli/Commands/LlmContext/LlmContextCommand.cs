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
                new OptionDescriptor("-o, --output", "string", "Output format: json, text, or plain. Defaults to auto-detection."),
            ],
            CommandGroups = BuildCommandGroups(),
            ConnectionInfo = new ConnectionInfoDescriptor
            {
                DefaultConnectionString = "chronicle://<client>:<secret>@localhost:35000",
                EnvironmentVariable = "CHRONICLE_CONNECTION_STRING",
                ConfigFile = CliConfiguration.GetConfigPath(),
                Precedence = ["--server flag", "CHRONICLE_CONNECTION_STRING env var", "config file", "default (localhost:35000)"],
            },
            Tips =
            [
                "Use --output json for machine-readable output; --output text for human-readable tables.",
                "Pipe commands with --output json to jq for filtering: cratis events get --output json | jq '.[] | .metadata'",
                "Set a default server with: cratis config set server chronicle://myhost:35000",
                "Most commands require --event-store and --namespace; both default to 'default'.",
                "Use 'cratis observers list --type reactor' to filter by observer type.",
            ],
        };

        Console.WriteLine(JsonSerializer.Serialize(descriptor, _serializerOptions));
        return Task.FromResult(ExitCodes.Success);
    }

    static IReadOnlyList<CommandGroupDescriptor> BuildCommandGroups() =>
    [
        new(
            "event-stores",
            "Manage event stores",
            [new CommandDescriptor("list", "List all event stores", null, null)]),
        new(
            "namespaces",
            "Manage namespaces within an event store",
            [new CommandDescriptor("list", "List namespaces in an event store", EventStoreOptions(), null)]),
        new(
            "event-types",
            "Manage event types",
            [new CommandDescriptor("list", "List registered event types", EventStoreOptions(), null)]),
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
                    ]),
                new CommandDescriptor(
                    "count",
                    "Get the tail sequence number",
                    EventStoreOptions(),
                    [new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)")]),
            ]),
        new(
            "observers",
            "Manage observers (reactors, reducers, projections)",
            [
                new CommandDescriptor(
                    "list",
                    "List observers",
                    EventStoreOptions(),
                    [new OptionDescriptor("-t, --type", "string", "Filter by type: reactor, reducer, projection, or all")]),
                new CommandDescriptor(
                    "replay",
                    "Replay an observer from the beginning",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "guid", "Observer identifier (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ]),
                new CommandDescriptor(
                    "replay-partition",
                    "Replay a specific partition of an observer",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "guid", "Observer identifier (positional)"),
                        new OptionDescriptor("<PARTITION>", "string", "Partition key (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ]),
                new CommandDescriptor(
                    "retry-partition",
                    "Retry a failed partition",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<OBSERVER_ID>", "guid", "Observer identifier (positional)"),
                        new OptionDescriptor("<PARTITION>", "string", "Partition key (positional)"),
                        new OptionDescriptor("--sequence", "string", "Event sequence name (default: event-log)"),
                    ]),
            ]),
        new(
            "failed-partitions",
            "Inspect failed observer partitions",
            [
                new CommandDescriptor(
                    "list",
                    "List failed partitions",
                    EventStoreOptions(),
                    [new OptionDescriptor("--observer", "string", "Filter by observer identifier")]),
            ]),
        new(
            "recommendations",
            "Manage system recommendations",
            [
                new CommandDescriptor("list", "List recommendations", EventStoreOptions(), null),
                new CommandDescriptor(
                    "perform",
                    "Perform a recommendation",
                    EventStoreOptions(),
                    [new OptionDescriptor("<RECOMMENDATION_ID>", "guid", "Recommendation identifier (positional)")]),
                new CommandDescriptor(
                    "ignore",
                    "Ignore a recommendation",
                    EventStoreOptions(),
                    [new OptionDescriptor("<RECOMMENDATION_ID>", "guid", "Recommendation identifier (positional)")]),
            ]),
        new(
            "identities",
            "Inspect identities",
            [new CommandDescriptor("list", "List known identities", EventStoreOptions(), null)]),
        new(
            "projections",
            "Manage projections",
            [
                new CommandDescriptor("list", "List projection definitions", EventStoreOptions(), null),
                new CommandDescriptor(
                    "show",
                    "Show a projection declaration",
                    EventStoreOptions(),
                    [new OptionDescriptor("<IDENTIFIER>", "string", "Projection identifier (positional)")]),
            ]),
        new(
            "read-models",
            "Inspect read model data",
            [
                new CommandDescriptor("list", "List read model definitions", EventStoreOptions(), null),
                new CommandDescriptor(
                    "instances",
                    "List read model instances",
                    EventStoreOptions(),
                    [
                        new OptionDescriptor("<READ_MODEL>", "string", "Read model identifier (positional)"),
                        new OptionDescriptor("--page", "int", "Page number, 1-based (default: 1)"),
                        new OptionDescriptor("--page-size", "int", "Items per page (default: 20)"),
                    ]),
            ]),
        new(
            "config",
            "Manage CLI configuration",
            [
                new CommandDescriptor("show", "Show current configuration", null, null),
                new CommandDescriptor(
                    "set",
                    "Set a configuration value",
                    null,
                    [
                        new OptionDescriptor("<KEY>", "string", "Key to set: server, event-store, or namespace (positional)"),
                        new OptionDescriptor("<VALUE>", "string", "Value to assign (positional)"),
                    ]),
                new CommandDescriptor("path", "Print configuration file path", null, null),
            ]),
    ];

    static IReadOnlyList<OptionDescriptor> EventStoreOptions() =>
    [
        new("-e, --event-store", "string", "Event store name (default: default)"),
        new("-n, --namespace", "string", "Namespace within the event store (default: default)"),
    ];
}
