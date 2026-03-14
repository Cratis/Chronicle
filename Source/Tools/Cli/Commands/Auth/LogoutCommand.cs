// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Auth;

/// <summary>
/// Clears the cached login token and user session.
/// </summary>
public class LogoutCommand : AsyncCommand<GlobalSettings>
{
    /// <inheritdoc/>
    public override Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();

        if (string.IsNullOrWhiteSpace(ctx.LoggedInUser) && string.IsNullOrWhiteSpace(ctx.AccessToken))
        {
            OutputFormatter.WriteMessage(format, "No active login session.");
            return Task.FromResult(ExitCodes.Success);
        }

        var user = ctx.LoggedInUser ?? "unknown";
        ctx.AccessToken = null;
        ctx.TokenExpiry = null;
        ctx.LoggedInUser = null;
        config.Save();

        OutputFormatter.WriteMessage(format, $"Logged out user '{user}'. Cached token cleared.");
        return Task.FromResult(ExitCodes.Success);
    }
}
