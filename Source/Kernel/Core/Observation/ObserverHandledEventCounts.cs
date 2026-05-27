// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverHandledEventCounts"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> used to look up observer state and event sequence counts.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <remarks>
/// The grain is keyed by <c>EventStoreName + EventStoreNamespaceName</c> via <see cref="ObserversKey"/>.
/// It maintains an in-memory cache of handled event counts so that the workbench can show the number
/// without paying the per-request cost of counting events in storage. The cache refreshes on a timer
/// (default 30 seconds) and lazily computes on first access; callers that need a fresh view can also
/// invoke <see cref="Refresh"/> directly.
/// <para>
/// Counts are derived from <c>GetCount</c> on the event sequence storage and span every event in the
/// sequence from the first sequence number through the observer's <c>LastHandledEventSequenceNumber</c>.
/// Redacted events are still present in the sequence at their original sequence number (redaction
/// rewrites the document content in place) and therefore continue to be included in the count —
/// the observer did process those events before they were redacted.
/// </para>
/// </remarks>
public class ObserverHandledEventCounts(
    IStorage storage,
    ILogger<ObserverHandledEventCounts> logger) : Grain, IObserverHandledEventCounts
{
    /// <summary>
    /// The default cadence at which the cache is refreshed. Chosen to keep the workbench numbers
    /// reasonably fresh without putting unnecessary load on storage in systems with many observers.
    /// </summary>
    public static readonly TimeSpan DefaultRefreshPeriod = TimeSpan.FromSeconds(30);

    readonly ConcurrentDictionary<ObserverHandledEventCountKey, EventCount> _counts = new();
    ObserversKey _observersKey = ObserversKey.NotSet;
    IGrainTimer? _refreshTimer;
    bool _isRefreshing;

    /// <inheritdoc/>
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _observersKey = ObserversKey.Parse(this.GetPrimaryKeyString());
        await SafeRefresh();
        _refreshTimer = this.RegisterGrainTimer(
            SafeRefresh,
            new GrainTimerCreationOptions { DueTime = DefaultRefreshPeriod, Period = DefaultRefreshPeriod });
    }

    /// <inheritdoc/>
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<EventCount> GetCountFor(ObserverId observerId, EventSequenceId eventSequenceId)
    {
        var key = new ObserverHandledEventCountKey(observerId, eventSequenceId);
        return Task.FromResult(_counts.TryGetValue(key, out var count) ? count : EventCount.Zero);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<ObserverHandledEventCountKey, EventCount>> GetAll() =>
        Task.FromResult<IReadOnlyDictionary<ObserverHandledEventCountKey, EventCount>>(
            new Dictionary<ObserverHandledEventCountKey, EventCount>(_counts));

    /// <inheritdoc/>
    public Task Refresh() => RefreshInternal();

    async Task SafeRefresh()
    {
        if (_isRefreshing)
        {
            return;
        }

        _isRefreshing = true;
        try
        {
            await RefreshInternal();
        }
        catch (Exception ex)
        {
            logger.FailedToRefreshHandledEventCounts(_observersKey.EventStore, _observersKey.Namespace, ex);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    async Task RefreshInternal()
    {
        var observerDefinitions = await storage.GetEventStore(_observersKey.EventStore).Observers.GetAll();
        var observerStates = await storage.GetEventStore(_observersKey.EventStore).GetNamespace(_observersKey.Namespace).Observers.GetAll();

        var join =
            from definition in observerDefinitions
            join state in observerStates on definition.Identifier equals state.Identifier
            select (Definition: definition, State: state);

        var seenKeys = new HashSet<ObserverHandledEventCountKey>();
        foreach (var (definition, state) in join)
        {
            var eventSequenceId = definition.EventSequenceId;
            var key = new ObserverHandledEventCountKey(definition.Identifier, eventSequenceId);
            seenKeys.Add(key);
            _counts[key] = await ComputeCount(eventSequenceId, state.LastHandledEventSequenceNumber);
        }

        // Remove cached entries that no longer correspond to any current observer.
        foreach (var existing in _counts.Keys.ToArray())
        {
            if (!seenKeys.Contains(existing))
            {
                _counts.TryRemove(existing, out _);
            }
        }
    }

    async Task<EventCount> ComputeCount(EventSequenceId eventSequenceId, EventSequenceNumber lastHandled)
    {
        if (!lastHandled.IsActualValue)
        {
            return EventCount.Zero;
        }

        return await storage
            .GetEventStore(_observersKey.EventStore)
            .GetNamespace(_observersKey.Namespace)
            .GetEventSequence(eventSequenceId)
            .GetCount(lastHandled);
    }
}
