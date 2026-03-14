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
        var ctx = config.GetCurrentContext();

        switch (settings.Key.ToLowerInvariant())
        {
            case "server":
                ctx.Server = settings.Value;
                break;
            case "event-store":
                ctx.EventStore = settings.Value;
                break;
            case "namespace":
                ctx.Namespace = settings.Value;
                break;
            case "client-id":
                ctx.ClientId = settings.Value;
                break;
            case "client-secret":
                ctx.ClientSecret = settings.Value;
                break;
            case "management-port":
                if (int.TryParse(settings.Value, out var port))
                {
                    ctx.ManagementPort = port;
                }
                else
                {
                    OutputFormatter.WriteError(format, $"Invalid port value: '{settings.Value}'", "management-port must be a valid integer.");
                    return Task.FromResult(ExitCodes.NotFound);
                }
                break;
            default:
                OutputFormatter.WriteError(format, $"Unknown config key: '{settings.Key}'", "Valid keys: server, event-store, namespace, client-id, client-secret, management-port");
                return Task.FromResult(ExitCodes.NotFound);
        }

        config.Save();
        OutputFormatter.WriteMessage(format, $"Set '{settings.Key}' to '{settings.Value}' in context '{config.ActiveContextName}'");
        return Task.FromResult(ExitCodes.Success);
    }
}
