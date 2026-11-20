// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IDormantTargetSweep"/>.
/// </summary>
/// <remarks>
/// Every read goes through <see cref="IDurableSubscriptionRecords"/>, which shares one read of a namespace between all that
/// ask within a short while, and nothing here keeps state per observer: the connections waiting for a sweep are the only thing
/// held, and only until it runs. An observer that is not told is found later by the safety net of its queue, so a sweep that
/// fails costs time, not correctness.
/// </remarks>
/// <param name="storage">The <see cref="IStorage"/> that lists the event stores, namespaces and observers.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for reaching the observers.</param>
/// <param name="records">The <see cref="IDurableSubscriptionRecords"/> that hold the targets.</param>
/// <param name="wakeAdmission">The <see cref="IWakeAdmission"/> that bounds the wakes of one silo.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[Singleton]
public class DormantTargetSweep(
    IStorage storage,
    IGrainFactory grainFactory,
    IDurableSubscriptionRecords records,
    IWakeAdmission wakeAdmission,
    ILogger<DormantTargetSweep> logger) : IDormantTargetSweep
{
    /// <summary>
    /// The most observers told at the same time by one sweep.
    /// </summary>
    internal const int MaxConcurrentNotifications = 16;

    readonly object _lock = new();
    readonly HashSet<ConnectionId> _leaving = [];
    readonly HashSet<ObserverKey> _told = [];
    Task _running = Task.CompletedTask;
    bool _sweeping;

    /// <summary>
    /// Gets or sets how long connections that leave are collected before they are swept together.
    /// </summary>
    internal TimeSpan CollectFor { get; set; } = TimeSpan.FromSeconds(1);

    /// <inheritdoc/>
    public Task ConnectionLeft(ConnectionId connectionId, IReadOnlyCollection<ObserverKey> alreadyNotified)
    {
        if (!records.CanBeRelied)
        {
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            _leaving.Add(connectionId);
            _told.UnionWith(alreadyNotified);
            if (!_sweeping)
            {
                _sweeping = true;
                _running = SweepConnections();
            }

            return _running;
        }
    }

    /// <inheritdoc/>
    public async Task<int> SilosLeft(IReadOnlyCollection<string> silos, CancellationToken cancellationToken = default)
    {
        if (silos.Count == 0 || !records.CanBeRelied)
        {
            return 0;
        }

        var held = await FindObserversHeldBy(target => silos.Contains(target.Silo));
        using var gate = new SemaphoreSlim(MaxConcurrentNotifications);
        await Task.WhenAll(held.Select(async observer =>
        {
            try
            {
                await gate.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await wakeAdmission.WaitAsync(cancellationToken);
                try
                {
                    // The watchdog pass of a woken observer validates the clients its record names and drops the ones that are gone.
                    await grainFactory.GetGrain<IObserver>(observer.Key).RunWatchdogOnce().WaitAsync(cancellationToken);
                }
                finally
                {
                    wakeAdmission.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Stopping.
            }
            catch (Exception ex)
            {
                logger.FailedToSweepDormantTarget(observer.Key.ObserverId, ex);
            }
            finally
            {
                gate.Release();
            }
        }));

        return held.Count;
    }

    async Task SweepConnections()
    {
        try
        {
            while (true)
            {
                await Task.Delay(CollectFor);
                ConnectionId[] leaving;
                HashSet<ObserverKey> told;
                lock (_lock)
                {
                    if (_leaving.Count == 0)
                    {
                        _sweeping = false;
                        return;
                    }

                    leaving = [.. _leaving];
                    told = [.. _told];
                    _leaving.Clear();
                    _told.Clear();
                }

                await Tell(leaving, told);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.FailedToSweepDormantTargets(ex);
            lock (_lock)
            {
                _sweeping = false;
            }
        }
    }

    async Task Tell(ConnectionId[] leaving, HashSet<ObserverKey> told)
    {
        var held = await FindObserversHeldBy(target => leaving.Any(connection => connection.Value == target.ConnectionId));
        using var gate = new SemaphoreSlim(MaxConcurrentNotifications);
        await Task.WhenAll(held.Where(observer => !told.Contains(observer.Key)).Select(async observer =>
        {
            await gate.WaitAsync();
            try
            {
                foreach (var connection in leaving.Where(connection => observer.Record.Targets.Any(target => target.ConnectionId == connection.Value)))
                {
                    await grainFactory.GetGrain<IObserver>(observer.Key).UnsubscribeIfMatchesClient(connection);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.FailedToSweepDormantTarget(observer.Key.ObserverId, ex);
            }
            finally
            {
                gate.Release();
            }
        }));
    }

    async Task<List<(ObserverKey Key, ObserverDurableSubscription Record)>> FindObserversHeldBy(Func<ObserverDurableSubscriptionTarget, bool> isTarget)
    {
        var held = new List<(ObserverKey, ObserverDurableSubscription)>();
        try
        {
            foreach (var eventStore in await storage.GetEventStores())
            {
                var eventStoreStorage = storage.GetEventStore(eventStore);
                var sequences = (await eventStoreStorage.Observers.GetAll())
                    .Where(definition => definition.Owner == ObserverOwner.Client)
                    .Select(definition => definition.EventSequenceId)
                    .Distinct()
                    .ToArray();
                foreach (var @namespace in (await eventStoreStorage.Namespaces.GetAll()).Select(state => state.Name))
                {
                    foreach (var sequence in sequences)
                    {
                        var stored = await records.GetAll(eventStore, @namespace, sequence);
                        held.AddRange(stored
                            .Where(entry => entry.Value.Targets.Any(isTarget))
                            .Select(entry => (new ObserverKey(entry.Key, eventStore, @namespace, sequence), entry.Value)));
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.FailedToSweepDormantTargets(ex);
        }

        return held;
    }
}
