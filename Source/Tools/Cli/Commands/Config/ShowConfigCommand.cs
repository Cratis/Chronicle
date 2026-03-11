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
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();

        OutputFormatter.WriteObject(format, config, cfg =>
        {
            AnsiConsole.MarkupLine($"[bold]Default Server:[/]      {OrNotSet(cfg.DefaultServer).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Default Event Store:[/] {OrNotSet(cfg.DefaultEventStore).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Default Namespace:[/]   {OrNotSet(cfg.DefaultNamespace).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Client ID:[/]           {OrNotSet(cfg.ClientId).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Client Secret:[/]       {(string.IsNullOrWhiteSpace(cfg.ClientSecret) ? "(not set)" : "********")}");
            AnsiConsole.MarkupLine($"[bold]Logged-in User:[/]      {OrNotSet(cfg.LoggedInUser).EscapeMarkup()}");
        });

        return Task.FromResult(ExitCodes.Success);
    }

    static string OrNotSet(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(not set)" : value;
}
