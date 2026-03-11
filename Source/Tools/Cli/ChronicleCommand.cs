// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Grpc.Core;
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
    public sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings)
    {
        var format = settings.ResolveOutputFormat();

        try
        {
            var connectionString = settings.ResolveConnectionString();
            var options = ChronicleOptions.FromConnectionString(new ChronicleConnectionString(connectionString));
            options.AutoDiscoverAndRegister = false;
            using var client = new ChronicleClient(options);
            return await ExecuteCommandAsync(client, settings, format);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            OutputFormatter.WriteError(format, "Cannot connect to Chronicle server", $"Verify the server is running and reachable. Connection: {settings.ResolveConnectionString()}");
            return 2;
        }
        catch (RpcException ex) when (ex.Status.Detail.Contains("disposed", StringComparison.OrdinalIgnoreCase))
        {
            OutputFormatter.WriteError(format, "Cannot connect to Chronicle server", $"Verify the server is running and reachable. Connection: {settings.ResolveConnectionString()}");
            return 2;
        }
        catch (RpcException ex)
        {
            OutputFormatter.WriteError(format, $"Server error: {ex.Status.Detail}");
            return 3;
        }
        catch (ObjectDisposedException)
        {
            OutputFormatter.WriteError(format, "Cannot connect to Chronicle server", $"Verify the server is running and reachable. Connection: {settings.ResolveConnectionString()}");
            return 2;
        }
    }

    /// <summary>
    /// Gets the gRPC services accessor from an event store connection.
    /// </summary>
    /// <param name="eventStore">The event store to get services from.</param>
    /// <returns>The <see cref="IServices"/> instance.</returns>
    protected static IServices GetServices(IEventStore eventStore)
        => ((IChronicleServicesAccessor)eventStore.Connection).Services;

    /// <summary>
    /// Executes the command logic with an established Chronicle client.
    /// </summary>
    /// <param name="client">The connected <see cref="IChronicleClient"/>.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="format">The resolved output format.</param>
    /// <returns>The exit code.</returns>
    protected abstract Task<int> ExecuteCommandAsync(IChronicleClient client, TSettings settings, string format);
}
