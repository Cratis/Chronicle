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
        var contextName = config.ActiveContextName;
        var ctx = config.GetCurrentContext();

        OutputFormatter.WriteObject(
            format,
            new
            {
                ActiveContext = contextName,
                AvailableContexts = config.Contexts.Keys.ToArray(),
                ctx.Server,
                ctx.EventStore,
                ctx.Namespace,
                ctx.ClientId,
                HasClientSecret = !string.IsNullOrWhiteSpace(ctx.ClientSecret),
                ctx.ManagementPort,
                ctx.LoggedInUser
            },
            _ =>
        {
            AnsiConsole.MarkupLine($"[bold]Current Context:[/]    {contextName.EscapeMarkup()}");
            if (config.Contexts.Count > 1)
            {
                AnsiConsole.MarkupLine($"[bold]Available Contexts:[/] {string.Join(", ", config.Contexts.Keys.Select(k => k == contextName ? $"[green]{k.EscapeMarkup()}[/]" : k.EscapeMarkup()))}");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Server:[/]             {OrNotSet(ctx.Server).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Event Store:[/]        {OrNotSet(ctx.EventStore).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Namespace:[/]          {OrNotSet(ctx.Namespace).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Client ID:[/]          {OrNotSet(ctx.ClientId).EscapeMarkup()}");
            AnsiConsole.MarkupLine($"[bold]Client Secret:[/]      {(string.IsNullOrWhiteSpace(ctx.ClientSecret) ? "(not set)" : "********")}");
            AnsiConsole.MarkupLine($"[bold]Management Port:[/]    {(ctx.ManagementPort.HasValue ? ctx.ManagementPort.Value.ToString() : "(default: 8080)")}");
            AnsiConsole.MarkupLine($"[bold]Logged-in User:[/]     {OrNotSet(ctx.LoggedInUser).EscapeMarkup()}");
        });

        return Task.FromResult(ExitCodes.Success);
    }

    static string OrNotSet(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(not set)" : value;
}
