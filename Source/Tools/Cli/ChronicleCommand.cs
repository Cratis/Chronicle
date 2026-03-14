// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using Cratis.Chronicle.Cli.Commands.Version;
using Cratis.Chronicle.Connections;
using Grpc.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Cratis.Chronicle.Cli;

/// <summary>
/// Base class for all CLI commands that need a Chronicle connection.
/// </summary>
/// <typeparam name="TSettings">The settings type for this command.</typeparam>
public abstract class ChronicleCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : GlobalSettings
{
    /// <inheritdoc/>
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        // Start update check in the background — never blocks the command.
        using var updateCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        updateCts.CancelAfter(TimeSpan.FromSeconds(5));
        var updateCheckTask = UpdateChecker.CheckForUpdate(VersionCommand.GetCliVersion(), updateCts.Token);

        try
        {
            var connectionString = new ChronicleConnectionString(settings.ResolveConnectionString());
            var managementPort = settings.ResolveManagementPort();
            using var client = CliServiceClient.Create(connectionString, managementPort);
            var exitCode = await ExecuteCommandAsync(client.Services, settings, format);

            await ShowUpdateHint(updateCheckTask, format);
            return exitCode;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, $"Verify the server is running and reachable. Connection: {settings.ResolveConnectionString()}");
            return ExitCodes.ConnectionError;
        }
        catch (RpcException ex) when (ex.Status.Detail.Contains("disposed", StringComparison.OrdinalIgnoreCase))
        {
            OutputFormatter.WriteError(format, "Server error", $"{ex.Status.Detail}");
            return ExitCodes.ServerError;
        }
        catch (RpcException ex)
        {
            OutputFormatter.WriteError(format, $"Server error: {ex.Status.Detail}");
            return ExitCodes.ServerError;
        }
        catch (ObjectDisposedException)
        {
            OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, $"Verify the server is running and reachable. Connection: {settings.ResolveConnectionString()}");
            return ExitCodes.ConnectionError;
        }
        catch (HttpRequestException ex)
        {
            OutputFormatter.WriteError(format, ex.InnerException is SocketException ? $"Connection refused ({settings.ResolveConnectionString()})" : ex.Message);
            return ExitCodes.ConnectionError;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OutputFormatter.WriteError(format, ex.Message);
            return ExitCodes.ServerError;
        }
    }

    /// <summary>
    /// Executes the command logic with gRPC services.
    /// </summary>
    /// <param name="services">The gRPC service proxies.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="format">The resolved output format.</param>
    /// <returns>The exit code.</returns>
    protected abstract Task<int> ExecuteCommandAsync(IServices services, TSettings settings, string format);

    static async Task ShowUpdateHint(Task<string?> updateCheckTask, string format)
    {
        try
        {
            var latestVersion = await updateCheckTask;
            if (latestVersion is null)
            {
                return;
            }

            if (format is OutputFormats.Text)
            {
                AnsiConsole.MarkupLine(string.Empty);
                AnsiConsole.MarkupLine($"[yellow]A newer version ({latestVersion.EscapeMarkup()}) is available. Run 'cratis update' to upgrade.[/]");
            }
        }
        catch
        {
            // Update check failures are non-critical.
        }
    }
}
