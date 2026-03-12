// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Context;

/// <summary>
/// Shows detailed information about the current or a specific named context.
/// </summary>
public class ShowContextCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var contextName = config.ActiveContextName;
        var ctx = config.GetCurrentContext();

        OutputFormatter.WriteObject(
            format,
            new
            {
                Context = contextName,
                ctx.Server,
                ctx.EventStore,
                ctx.Namespace,
                ctx.ClientId,
                HasClientSecret = !string.IsNullOrWhiteSpace(ctx.ClientSecret),
                ctx.LoggedInUser
            },
            _ =>
        {
            AnsiConsole.MarkupLine($"[bold]Context:[/]        {contextName.EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Server:[/]         {OrNotSet(ctx.Server).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Event Store:[/]    {OrNotSet(ctx.EventStore).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Namespace:[/]      {OrNotSet(ctx.Namespace).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Client ID:[/]      {OrNotSet(ctx.ClientId).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Client Secret:[/]  {(string.IsNullOrWhiteSpace(ctx.ClientSecret) ? "(not set)" : "********")}");
            AnsiConsole.MarkupLine($"[bold]Logged-in User:[/] {OrNotSet(ctx.LoggedInUser).EscapeMarkup()}");
        });

        return Task.FromResult(ExitCodes.Success);
    }

    static string OrNotSet(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(not set)" : value;
}
