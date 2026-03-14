// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Auth;

/// <summary>
/// Shows the current authentication status including login session and client credentials.
/// </summary>
public class AuthStatusCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var contextName = config.ActiveContextName;
        var ctx = config.GetCurrentContext();

        var status = new AuthStatusInfo
        {
            Context = contextName,
            LoggedInUser = ctx.LoggedInUser,
            HasToken = !string.IsNullOrWhiteSpace(ctx.AccessToken),
            TokenExpiry = ctx.TokenExpiry,
            IsTokenValid = IsTokenValid(ctx),
            ClientId = ctx.ClientId,
            HasClientSecret = !string.IsNullOrWhiteSpace(ctx.ClientSecret),
            Server = ctx.Server
        };

        OutputFormatter.WriteObject(format, status, s =>
        {
            AnsiConsole.MarkupLine($"[bold]Context:[/]         {s.Context.EscapeMarkup()}");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Login Session:[/]");
            if (!string.IsNullOrWhiteSpace(s.LoggedInUser))
            {
                AnsiConsole.MarkupLine($"  User:          {s.LoggedInUser.EscapeMarkup()}");
                string tokenStatus;
                if (!s.HasToken)
                {
                    tokenStatus = "[dim]none[/]";
                }
                else
                {
                    tokenStatus = s.IsTokenValid ? "[green]valid[/]" : "[red]expired[/]";
                }

                AnsiConsole.MarkupLine($"  Token:         {tokenStatus}");
                if (s.TokenExpiry is not null)
                {
                    AnsiConsole.MarkupLine($"  Expires:       {s.TokenExpiry}");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("  [dim]Not logged in[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Client Credentials:[/]");
            if (!string.IsNullOrWhiteSpace(s.ClientId))
            {
                AnsiConsole.MarkupLine($"  Client ID:     {s.ClientId.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"  Client Secret: {(s.HasClientSecret ? "[dim]********[/]" : "[dim](not set)[/]")}");
            }
            else
            {
                AnsiConsole.MarkupLine("  [dim]Not configured[/]");
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[bold]Server:[/]          {(s.Server ?? "(not set)").EscapeMarkup()}");
        });

        return Task.FromResult(ExitCodes.Success);
    }

    static bool IsTokenValid(CliContext ctx)
    {
        if (string.IsNullOrWhiteSpace(ctx.AccessToken) || string.IsNullOrWhiteSpace(ctx.TokenExpiry))
        {
            return false;
        }

        return DateTimeOffset.TryParse(ctx.TokenExpiry, out var expiry) && DateTimeOffset.UtcNow < expiry;
    }

    sealed record AuthStatusInfo
    {
        public string Context { get; init; } = string.Empty;
        public string? LoggedInUser { get; init; }
        public bool HasToken { get; init; }
        public string? TokenExpiry { get; init; }
        public bool IsTokenValid { get; init; }
        public string? ClientId { get; init; }
        public bool HasClientSecret { get; init; }
        public string? Server { get; init; }
    }
}
