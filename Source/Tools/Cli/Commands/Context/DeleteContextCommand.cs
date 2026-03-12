// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Deletes a named context. Cannot delete the currently active context.
/// </summary>
public class DeleteContextCommand : AsyncCommand<ContextNameSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, ContextNameSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        if (!config.Contexts.ContainsKey(settings.Name))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.Name}' does not exist");
            return Task.FromResult(ExitCodes.NotFound);
        }

        if (config.ActiveContextName == settings.Name)
        {
            OutputFormatter.WriteError(format, $"Cannot delete the active context '{settings.Name}'", "Switch to a different context first with: cratis context set <other>");
            return Task.FromResult(ExitCodes.ValidationError);
        }

        config.Contexts.Remove(settings.Name);
        config.Save();

        OutputFormatter.WriteMessage(format, $"Deleted context '{settings.Name}'.");
        return Task.FromResult(ExitCodes.Success);
    }
}
