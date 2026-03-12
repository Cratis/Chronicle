// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Connections;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli.Commands.Auth;

/// <summary>
/// Authenticates a user via the password grant flow and caches the resulting token.
/// </summary>
public class LoginCommand : AsyncCommand<LoginSettings>
{
    const int DefaultManagementPort = 8080;

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, LoginSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        string password;
        if (!string.IsNullOrWhiteSpace(settings.Password))
        {
            password = settings.Password;
        }
        else
        {
            try
            {
                password = AnsiConsole.Prompt(
                    new TextPrompt<string>("Password:")
                        .PromptStyle("dim")
                        .Secret());
            }
            catch (InvalidOperationException)
            {
                OutputFormatter.WriteError(format, "Interactive terminal required", "The login command requires an interactive terminal for secure password entry. Use --password for non-interactive login.");
                return ExitCodes.AuthenticationError;
            }
        }

        try
        {
            var connectionString = new ChronicleConnectionString(settings.ResolveConnectionString());
            var disableTls = connectionString.DisableTls;
            var scheme = disableTls ? "http" : "https";
            var tokenEndpoint = $"{scheme}://{connectionString.ServerAddress.Host}:{DefaultManagementPort}/connect/token";

            using var handler = CreateHandler();
            using var httpClient = new HttpClient(handler);
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = settings.Username,
                ["password"] = password
            });

            var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                OutputFormatter.WriteError(format, "Login failed", $"Server returned {(int)response.StatusCode}: {errorBody}");
                return ExitCodes.AuthenticationError;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            var accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var expiresInProp) ? expiresInProp.GetInt32() : 3600;
            var expiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

            var config = CliConfiguration.Load();
            var ctx = config.GetCurrentContext();
            ctx.AccessToken = accessToken;
            ctx.TokenExpiry = expiry.ToString("O");
            ctx.LoggedInUser = settings.Username;
            config.Save();

            OutputFormatter.WriteMessage(format, $"Logged in as '{settings.Username}'. Token expires at {expiry:u}.");
            return ExitCodes.Success;
        }
        catch (HttpRequestException ex)
        {
            OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, ex.Message);
            return ExitCodes.ConnectionError;
        }
    }

#pragma warning disable MA0039 // Do not write your own certificate validation method
    static HttpClientHandler CreateHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
    }
#pragma warning restore MA0039
}
