// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Imports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Keeps import staging bounded by <see cref="Configuration.Imports.StagingRetention"/>: abandons imports that stayed open
/// without activity, fails imports whose commit stopped making progress, discards the staging of ended imports and
/// removes ended imports once they are older than the retention.
/// </summary>
/// <param name="storage">The <see cref="IStorage"/> to find imports in.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The logger.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
/// <remarks>
/// Every silo sweeps; each change is decided by the import itself, so a sweep that races another changes nothing.
/// Staged chunks hold event content as received, personal data included, so none outlives the import it belongs to.
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
    /// <returns>How many imports the sweep ended or removed.</returns>
    public async Task<int> Sweep()
    {
        var swept = 0;
        foreach (var eventStore in await storage.GetEventStores())
        {
            var eventStoreStorage = storage.GetEventStore(eventStore);
            foreach (var @namespace in await eventStoreStorage.Namespaces.GetAll())
            {
                try
                {
                    var imports = eventStoreStorage.GetNamespace(@namespace.Name).Imports;
                    foreach (var import in await imports.GetAll())
                    {
                        if (await Sweep(imports, grainFactory.GetGrain<IImport>(new ImportKey(eventStore, @namespace.Name, import.Id)), import)) swept++;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.SweepFailed(eventStore, @namespace.Name, ex);
                }
            }
        }

        return swept;
    }

    async Task<bool> Sweep(IImportStorage imports, IImport grain, ImportRecord import)
    {
        var expired = _time.GetUtcNow() - import.LastActivity > options.Value.Imports.StagingRetention;
        if (import.State.AcceptsChunks()) return expired && await grain.AbandonIfExpired();
        if (!import.State.IsTerminal()) return expired && await grain.FailIfStalled();
        if (expired) return await grain.RemoveIfExpired();

        // An ended import never needs its staging. A chunk written while the import ended is removed here.
        if ((await imports.GetChunkSummaries(import.Id)).Any()) await imports.RemoveChunks(import.Id);
        return false;
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
