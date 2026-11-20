// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Removes client reactors and reducers that no client has held a subscription to for
/// <see cref="Observers.UnseenRemovalAfter"/>, as decided in decision 0035.
/// </summary>
/// <remarks>
/// The sweep reads storage, not grains: it lists the client-owned definitions of an event store, reads the recorded
/// last-seen time of each batch of them in every namespace, and only activates an observer that is a candidate. An
/// observer with no recorded time never is: reading it records the current time, which starts its clock. The removal
/// itself is <see cref="IObserverRemover"/>, so the same records go and the same refusals apply. Every silo runs the
/// sweep; it is idempotent, so concurrent sweeps need no coordination.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for reading definitions and last-seen times.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for the guards that need an observer's live state.</param>
/// <param name="remover">The <see cref="IObserverRemover"/> that removes an observer and everything keyed to it.</param>
/// <param name="options">The <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger{T}"/>.</param>
/// <param name="meter">The Chronicle <see cref="IMeter{T}"/>.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
/// <param name="durableSubscriptionRecords">The optional <see cref="IDurableSubscriptionRecords"/>; an observer released while dormant is seen through its recorded subscription, never by waking it.</param>
public sealed class UnseenObserverRemoval(
    IStorage storage,
    IGrainFactory grainFactory,
    IObserverRemover remover,
    IOptions<ChronicleOptions> options,
    ILogger<UnseenObserverRemoval> logger,
    [FromKeyedServices(WellKnown.MeterName)] IMeter<UnseenObserverRemoval> meter,
    TimeProvider? timeProvider = null,
    IDurableSubscriptionRecords? durableSubscriptionRecords = null) : ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    const int DefaultBatchSize = 100;
    static readonly TimeSpan _defaultSweepInterval = TimeSpan.FromHours(1);

    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    readonly Counter<long>? _removed = meter.ActualMeter?.CreateCounter<long>(
        "chronicle.observers.unseen_removed",
        "{observer}",
        "Client observers removed because no client had subscribed to them for the configured period");
    readonly ConcurrentDictionary<(EventStoreName EventStore, ObserverId ObserverId), byte> _removing = new();
    CancellationTokenSource? _cancellation;
    Task _loop = Task.CompletedTask;

    /// <inheritdoc/>
    public void Dispose() => _cancellation?.Dispose();

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(UnseenObserverRemoval), ServiceLifecycleStage.Active, Start, Stop);

    /// <summary>
    /// Runs one sweep over every event store.
    /// </summary>
    /// <returns>The number of observers removed.</returns>
    public async Task<int> Sweep()
    {
        var settings = options.Value.Observers;
        if (!settings.UnseenRemovalEnabled)
        {
            return 0;
        }

        var removed = 0;
        foreach (var eventStore in await storage.GetEventStores())
        {
            try
            {
                removed += await Sweep(eventStore, settings);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.SweepFailed(eventStore, ex);
            }
        }

        return removed;
    }

    async Task<int> Sweep(EventStoreName eventStore, Observers settings)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var candidates = (await eventStoreStorage.Observers.GetAll())
            .Where(definition => definition.Owner == ObserverOwner.Client && definition.Type is ObserverType.Reactor or ObserverType.Reducer)
            .ToArray();
        if (candidates.Length == 0)
        {
            return 0;
        }

        var namespaces = (await eventStoreStorage.Namespaces.GetAll()).Select(@namespace => @namespace.Name).ToArray();
        var batchSize = settings.UnseenRemovalBatchSize < 1 ? DefaultBatchSize : settings.UnseenRemovalBatchSize;
        var now = _time.GetUtcNow();
        var period = settings.UnseenRemovalAfter > TimeSpan.Zero ? settings.UnseenRemovalAfter : Observers.DefaultUnseenRemovalAfter;
        var cutoff = now - period;
        var removed = 0;

        foreach (var batch in candidates.Chunk(batchSize))
        {
            var ids = batch.Select(definition => definition.Identifier).ToArray();
            var lastSeen = new Dictionary<EventStoreNamespaceName, IReadOnlyDictionary<ObserverId, DateTimeOffset>>();
            foreach (var @namespace in namespaces)
            {
                lastSeen[@namespace] = await eventStoreStorage.GetNamespace(@namespace).Observers.GetLastSeenAt(ids, now);
            }

            foreach (var definition in batch)
            {
                var withState = namespaces.Where(@namespace => lastSeen[@namespace].ContainsKey(definition.Identifier)).ToArray();

                // Without any state record there is nowhere to hold a clock, so there is nothing to time out.
                if (withState.Length == 0 || withState.Any(@namespace => lastSeen[@namespace][definition.Identifier] > cutoff))
                {
                    continue;
                }

                try
                {
                    if (await Remove(eventStore, definition, withState, now))
                    {
                        removed++;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.RemovalFailed(eventStore, definition.Identifier, ex);
                }
            }
        }

        return removed;
    }

    async Task<bool> Remove(EventStoreName eventStore, Storage.Observation.ObserverDefinition definition, EventStoreNamespaceName[] namespaces, DateTimeOffset now)
    {
        // Two sweeps on a silo can reach the same candidate at the same time. The one that got there first removes it; the other leaves
        // it alone, so the removal is reported, logged and counted once. Sweeps on different silos are not coordinated by this.
        var key = (eventStore, definition.Identifier);
        if (!_removing.TryAdd(key, 0))
        {
            return false;
        }

        try
        {
            return await RemoveWhenRemovable(eventStore, definition, namespaces, now);
        }
        finally
        {
            _removing.TryRemove(key, out _);
        }
    }

    async Task<bool> RemoveWhenRemovable(EventStoreName eventStore, Storage.Observation.ObserverDefinition definition, EventStoreNamespaceName[] namespaces, DateTimeOffset now)
    {
        foreach (var @namespace in namespaces)
        {
            if (!await IsRemovableIn(eventStore, @namespace, definition, now))
            {
                return false;
            }
        }

        var result = await remover.Remove(eventStore, definition.Identifier, definition.EventSequenceId);
        if (result.Outcome != ObserverRemovalOutcome.Removed)
        {
            return false;
        }

        foreach (var @namespace in namespaces)
        {
            logger.RemovedUnseenObserver(eventStore, @namespace, definition.Identifier);
        }

        _removed?.Add(1);
        return true;
    }

    async Task<bool> IsRemovableIn(EventStoreName eventStore, EventStoreNamespaceName @namespace, Storage.Observation.ObserverDefinition definition, DateTimeOffset now)
    {
        var observerKey = new ObserverKey(definition.Identifier, eventStore, @namespace, definition.EventSequenceId);
        var observer = grainFactory.GetGrain<IObserver>(observerKey);
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);

        // An observer released while dormant has a durable subscription, so a client holds it: it is seen, and asking it
        // would wake it once every sweep. A record that outlived its client is dropped when the observer is next woken,
        // by its queue's safety net, and the clock starts from that time.
        if (await IsHeldByDurableSubscription(observerKey))
        {
            await namespaceStorage.Observers.SaveLastSeenAt(definition.Identifier, now, definition.EventSequenceId);
            return false;
        }

        if (await observer.IsSubscribed())
        {
            // Seen right now: an observer subscribed through a node that does not record it must not be asked about every sweep.
            await namespaceStorage.Observers.SaveLastSeenAt(definition.Identifier, now, definition.EventSequenceId);
            return false;
        }

        if ((await observer.GetState()).RunningState != ObserverRunningState.Disconnected)
        {
            return false;
        }

        if ((await namespaceStorage.FailedPartitions.GetFor(definition.Identifier)).Partitions.Any())
        {
            return false;
        }

        var jobs = await grainFactory.GetJobsManager(eventStore, @namespace).GetUnfinishedJobs();
        return !jobs.Any(job => job.Request is IObserverJobRequest request && request.ObserverKey.ObserverId == definition.Identifier);
    }

    async Task<bool> IsHeldByDurableSubscription(ObserverKey observerKey)
    {
        if (durableSubscriptionRecords?.CanBeRelied != true)
        {
            return false;
        }

        try
        {
            return await durableSubscriptionRecords.Get(observerKey) is { Targets.Count: > 0 };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Without the record the observer is asked, as it was before it could be released.
            return false;
        }
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
            var interval = options.Value.Observers.UnseenRemovalSweepInterval;
            if (interval <= TimeSpan.Zero)
            {
                interval = _defaultSweepInterval;
            }

            // Spread the silos so that two of them rarely sweep at the same moment.
            await Task.Delay(TimeSpan.FromTicks((long)(interval.Ticks * 0.1 * Random.Shared.NextDouble())), _time, cancellationToken);

            using var timer = new PeriodicTimer(interval, _time);
            do
            {
                try
                {
                    await Sweep();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.SweepFailed(string.Empty, ex);
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Stopping.
        }
    }
}
