// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Lists all configured contexts, marking the current one.
/// </summary>
public class ListContextsCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var currentName = config.ActiveContextName;

        var contexts = config.Contexts.Select(kvp => new ContextListEntry
        {
            Name = kvp.Key,
            Server = kvp.Value.Server ?? "(not set)",
            EventStore = kvp.Value.EventStore ?? "(not set)",
            IsCurrent = kvp.Key == currentName,
            LoggedInUser = kvp.Value.LoggedInUser
        }).ToArray();

        if (contexts.Length == 0)
        {
            OutputFormatter.WriteMessage(format, "No contexts configured. Create one with: cratis context create <name> --server <connection-string>");
            return Task.FromResult(ExitCodes.Success);
        }

        OutputFormatter.Write(
            format,
            contexts,
            ["Current", "Name", "Server", "Event Store", "User"],
            entry =>
            [
                entry.IsCurrent ? "*" : string.Empty,
                entry.Name,
                entry.Server,
                entry.EventStore,
                entry.LoggedInUser ?? "-"
            ]);

        return Task.FromResult(ExitCodes.Success);
    }

    sealed record ContextListEntry
    {
        public string Name { get; init; } = string.Empty;
        public string Server { get; init; } = string.Empty;
        public string EventStore { get; init; } = string.Empty;
        public bool IsCurrent { get; init; }
        public string? LoggedInUser { get; init; }
    }
}
