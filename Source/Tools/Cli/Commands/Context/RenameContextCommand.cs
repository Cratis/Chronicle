// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Renames an existing context.
/// </summary>
public class RenameContextCommand : AsyncCommand<RenameContextSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, RenameContextSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        if (!config.Contexts.TryGetValue(settings.OldName, out var ctx))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.OldName}' does not exist");
            return Task.FromResult(ExitCodes.NotFound);
        }

        if (config.Contexts.ContainsKey(settings.NewName))
        {
            OutputFormatter.WriteError(format, $"Context '{settings.NewName}' already exists");
            return Task.FromResult(ExitCodes.ValidationError);
        }

        config.Contexts.Remove(settings.OldName);
        config.Contexts[settings.NewName] = ctx;

        if (config.ActiveContext == settings.OldName)
        {
            config.ActiveContext = settings.NewName;
        }

        config.Save();

        OutputFormatter.WriteMessage(format, $"Renamed context '{settings.OldName}' to '{settings.NewName}'.");
        return Task.FromResult(ExitCodes.Success);
    }
}
