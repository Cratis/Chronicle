// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Reports the effective Chronicle client configuration when the host starts and connects in the background once it has started.
/// </summary>
/// <remarks>
/// Nothing here ever fails the host: a kernel that cannot be reached is reported and the connection keeps retrying on its own.
/// Credentials are never logged; only the configuration keys that supply them are named.
/// </remarks>
/// <param name="options">The <see cref="ChronicleClientOptions"/> in effect.</param>
/// <param name="client">The <see cref="IChronicleClient"/> to connect.</param>
/// <param name="environment">The <see cref="IHostEnvironment"/> the host runs in.</param>
/// <param name="lifetime">The <see cref="IHostApplicationLifetime"/> signalling when the host has started.</param>
/// <param name="logger">The <see cref="ILogger"/> to report to.</param>
/// <param name="shouldConnect">Decides, once the host has started, whether this service should connect; the host may connect on its own.</param>
/// <param name="onStarted">Optional extra diagnostics a host integration wants reported at startup.</param>
internal sealed class ChronicleClientStartup(
    ChronicleClientOptions options,
    IChronicleClient client,
    IHostEnvironment environment,
    IHostApplicationLifetime lifetime,
    ILogger<ChronicleClientStartup> logger,
    Func<bool> shouldConnect,
    Action<ILogger>? onStarted = null) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.UsingEventStore(options.EventStore.Value);

        var usesDevelopmentConnection = options.ConnectionString.ToString() == ChronicleConnectionString.Development.ToString();
        if (usesDevelopmentConnection && !environment.IsDevelopment())
        {
            logger.DevelopmentConnectionOutsideDevelopment(environment.EnvironmentName);
        }

        lifetime.ApplicationStarted.Register(() => _ = Task.Run(() => ConnectInBackground(lifetime.ApplicationStopping)));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    async Task ConnectInBackground(CancellationToken stopping)
    {
        try
        {
            onStarted?.Invoke(logger);
            if (!shouldConnect())
            {
                return;
            }

            var eventStore = await client.GetEventStore(options.EventStore);
            await eventStore.Connection.Connect().WaitAsync(stopping);
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.CouldNotConnectOnStartup(ex);
        }
    }
}
