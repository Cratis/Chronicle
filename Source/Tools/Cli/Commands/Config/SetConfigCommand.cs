// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Config;

/// <summary>
/// Sets a configuration value.
/// </summary>
public class SetConfigCommand : AsyncCommand<SetConfigSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, SetConfigSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        switch (settings.Key.ToLowerInvariant())
        {
            case "server":
                config.DefaultServer = settings.Value;
                break;
            case "event-store":
                config.DefaultEventStore = settings.Value;
                break;
            case "namespace":
                config.DefaultNamespace = settings.Value;
                break;
            default:
                OutputFormatter.WriteError(format, $"Unknown config key: '{settings.Key}'", "Valid keys: server, event-store, namespace");
                return Task.FromResult(ExitCodes.NotFound);
        }

        config.Save();
        OutputFormatter.WriteMessage(format, $"Set '{settings.Key}' to '{settings.Value}'");
        return Task.FromResult(ExitCodes.Success);
    }
}
