// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Config;

/// <summary>
/// Shows the current CLI configuration.
/// </summary>
public class ShowConfigCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        OutputFormatter.WriteObject(format, config, cfg =>
        {
            AnsiConsole.MarkupLine($"[bold]Default Server:[/]      {(cfg.DefaultServer ?? "(not set)").EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Default Event Store:[/] {(cfg.DefaultEventStore ?? "(not set)").EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Default Namespace:[/]   {(cfg.DefaultNamespace ?? "(not set)").EscapeMarkup()}");
        });

        return Task.FromResult(ExitCodes.Success);
    }
}
