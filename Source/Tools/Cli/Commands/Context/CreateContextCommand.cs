// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Creates a new named context. If it is the first context, it becomes the current context automatically.
/// </summary>
public class CreateContextCommand : AsyncCommand<CreateContextSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, CreateContextSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        if (config.Contexts.ContainsKey(settings.Name))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.Name}' already exists", "Use 'cratis config set' to update it, or 'cratis context delete' and recreate.");
            return Task.FromResult(ExitCodes.ValidationError);
        }

        config.Contexts[settings.Name] = new CliContext
        {
            Server = settings.Server,
            EventStore = settings.EventStore,
            Namespace = settings.Namespace
        };

        // If this is the only context (or no current is set), auto-switch to it.
        if (config.Contexts.Count == 1 || string.IsNullOrWhiteSpace(config.ActiveContext))
        {
            config.ActiveContext = settings.Name;
        }

        config.Save();

        var message = config.ActiveContext == settings.Name
            ? $"Created and switched to context '{settings.Name}'."
            : $"Created context '{settings.Name}'. Switch to it with: cratis context set {settings.Name}";

        OutputFormatter.WriteMessage(format, message);
        return Task.FromResult(ExitCodes.Success);
    }
}
