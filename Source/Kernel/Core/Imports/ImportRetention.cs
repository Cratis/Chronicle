// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Abandons imports that have stayed open past <see cref="Configuration.Imports.StagingRetention"/> without activity.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to find imports in.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
/// <remarks>
/// Every silo sweeps; abandoning is decided by the import itself, so a sweep that races another changes nothing.
/// </remarks>
public sealed class ImportRetention(
    IStorage storage,
    IGrainFactory grainFactory,
    IOptions<ChronicleOptions> options,
    ILogger<ImportRetention> logger,
    TimeProvider? timeProvider = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    CancellationTokenSource? _cancellation;
    Task _loop = Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose() => _cancellation?.Dispose();

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(ImportRetention), ServiceLifecycleStage.Active, Start, Stop);

    /// <summary>
    /// Sweeps every namespace of every event store once.
    /// </summary>
    /// <returns>How many imports were abandoned.</returns>
    public async Task<int> Sweep()
    {
        var abandoned = 0;
        var retention = options.Value.Imports.StagingRetention;
        foreach (var eventStore in await storage.GetEventStores())
        {
            var eventStoreStorage = storage.GetEventStore(eventStore);
            foreach (var @namespace in await eventStoreStorage.Namespaces.GetAll())
            {
                try
                {
                    var imports = await eventStoreStorage.GetNamespace(@namespace.Name).Imports.GetAll();
                    foreach (var import in imports.Where(_ => _.State.AcceptsChunks() && _time.GetUtcNow() - _.LastActivity > retention))
                    {
                        if (await grainFactory.GetGrain<IImport>(new ImportKey(eventStore, @namespace.Name, import.Id)).AbandonIfExpired())
                        {
                            abandoned++;
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.SweepFailed(eventStore, @namespace.Name, ex);
                }
            }
        }

        return abandoned;
    }

    Task Start(CancellationToken cancellationToken)
    {
        _cancellation = new();
        _loop = DetachedExecution.Run(() => Loop(_cancellation.Token));
        return Task.CompletedTask;
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        if (_cancellation is null) return;
        await _cancellation.CancelAsync();
        try
        {
            await _loop.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Stopping: the sweep is abandoned and runs again on the next start.
        }
    }

    async Task Loop(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(options.Value.Imports.RetentionSweepInterval, _time);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await Sweep();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.SweepFailed(string.Empty, string.Empty, ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping.
        }
    }
}
