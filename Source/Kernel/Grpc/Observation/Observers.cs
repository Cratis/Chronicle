// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reactive.Linq;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

// Cratis.Chronicle.Observation cannot be imported wholesale here: it declares the kernel-side command records that
// share their names with the contract types this file is built around, starting with RemoveObserver.
using IObserverRemover = Cratis.Chronicle.Observation.IObserverRemover;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
/// <param name="observerRemover">The <see cref="IObserverRemover"/> for removing observers.</param>
internal sealed class Observers(IGrainFactory grainFactory, IStorage storage, IObserverRemover observerRemover) : IObservers
{
    const int ObserverCompletionPollingDelayMs = 50;
    const ulong MaximumObserverCompletionRange = 10_000;

    /// <inheritdoc/>
    public async Task<RetryPartitionResponse> RetryPartition(RetryPartition command, CallContext context = default)
    {
        var outcome = await grainFactory.GetObserver(command).TryStartRecoverJobForFailedPartition(command.Partition);
        return new RetryPartitionResponse { Outcome = (PartitionRecoveryOutcome)(int)outcome };
    }

    /// <inheritdoc/>
    public async Task<ClearPartitionQuarantineResponse> ClearPartitionQuarantine(ClearPartitionQuarantine command, CallContext context = default)
    {
        var result = await grainFactory.GetObserver(command).ClearPartitionQuarantine(command.Partition, command.RetryImmediately);
        return new ClearPartitionQuarantineResponse
        {
            Outcome = (ClearPartitionQuarantineOutcome)(int)result.Outcome,
            RetryOutcome = (PartitionRecoveryOutcome)(int)result.RetryOutcome
        };
    }

    /// <inheritdoc/>
    public async Task<ReplayResponse> Replay(Replay command, CallContext context = default)
    {
        var jobId = await grainFactory.GetObserver(command).Replay();
        return new ReplayResponse { JobId = jobId.Value.ToString() };
    }

    /// <inheritdoc/>
    public Task ReplayPartition(ReplayPartition command, CallContext context = default) =>
        grainFactory.GetObserver(command).ReplayPartition(command.Partition);

    /// <inheritdoc/>
    public async Task<WaitForObserverCompletionResponse> WaitForCompletion(WaitForObserverCompletionRequest request, CallContext context = default)
    {
        var eventTypeTails = request.EventTypeTails
            .GroupBy(_ => _.EventType.Id, StringComparer.Ordinal)
            .ToDictionary(_ => _.Key, _ => _.Max(tail => tail.SequenceNumber), StringComparer.Ordinal);

        var stopwatch = Stopwatch.StartNew();

        // Recheck the subscription on every poll, but retain a matching event for unchanged filters
        // and event types. An unsubscribed observer or an unknown match cannot be cached.
        var lastMatchingEvents = new Dictionary<string, CachedMatchingEvent>();
        while (true)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var observers = (await GetObservers(
                new AllObserversRequest
                {
                    EventStore = request.EventStore,
                    Namespace = request.Namespace
                },
                context))
                .Where(_ => _.EventSequenceId == request.EventSequenceId &&
                    (eventTypeTails.Count == 0 || !_.EventTypes.Any() || _.EventTypes.Any(type => eventTypeTails.ContainsKey(type.Id))))
                .ToArray();

            if (observers.Length == 0)
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = true
                };
            }

            var observerIds = observers.Select(_ => (Concepts.Observation.ObserverId)_.Id).ToArray();
            var failedPartitions = await storage
                .GetEventStore(request.EventStore)
                .GetNamespace(request.Namespace)
                .FailedPartitions
                .GetFor(observerIds);
            var failedObserverIds = failedPartitions.Partitions.Select(_ => _.ObserverId.Value).ToHashSet(StringComparer.Ordinal);
            var undecidedObservers = observers.Where(_ => NeedsSubscription(_, eventTypeTails, request.TailEventSequenceNumber, failedObserverIds)).ToArray();
            var subscriptions = await Task.WhenAll(undecidedObservers.Select(observer =>
                grainFactory.GetGrain<Cratis.Chronicle.Observation.IObserver>(
                    new Concepts.Observation.ObserverKey(observer.Id, request.EventStore, request.Namespace, request.EventSequenceId)).GetSubscription()));
            var matchingObserverIds = new HashSet<string>(StringComparer.Ordinal);
            var outstanding = new List<string>();
            for (var index = 0; index < undecidedObservers.Length; index++)
            {
                var observer = undecidedObservers[index];
                var subscription = subscriptions[index];

                // Internal kernel reactors may intentionally have no subscription in this namespace.
                // Kernel-owned projections and external observers still need to handle the append,
                // even before they subscribe. Disconnected client observers must also be awaited.
                if (observer is { Owner: ObserverOwner.Kernel, Type: ObserverType.Reactor } && subscription is not { IsSubscribed: true })
                {
                    continue;
                }

                var subscribedEventTypes = subscription is { IsSubscribed: true }
                    ? subscription.EventTypes.Select(_ => _.Id.Value).ToArray()
                    : observer.EventTypes.Select(_ => _.Id).ToArray();
                if (eventTypeTails.Count > 0 && subscribedEventTypes.Length > 0 && !subscribedEventTypes.Any(eventTypeTails.ContainsKey))
                {
                    continue;
                }

                var target = (EventSequenceNumber)(eventTypeTails.Count == 0 || subscribedEventTypes.Length == 0
                    ? request.TailEventSequenceNumber
                    : subscribedEventTypes.Where(eventTypeTails.ContainsKey).Max(_ => eventTypeTails[_]));
                var lastHandled = (EventSequenceNumber)observer.LastHandledEventSequenceNumber;
                var hasFailedPartitions = failedObserverIds.Contains(observer.Id);
                if (!hasFailedPartitions && lastHandled.IsActualValue && lastHandled >= target)
                {
                    continue;
                }

                var lastMatchingEvent = (EventSequenceNumber?)target;

                if (subscription is { IsSubscribed: true, Filters: { } filters } && HasEffectiveFilters(filters) && HasBoundedAppendRange(request, target))
                {
                    if (lastMatchingEvents.TryGetValue(observer.Id, out var cached) &&
                        cached.IncludesHandledEvents == hasFailedPartitions &&
                        cached.LastMatchingEvent <= target &&
                        cached.Filters.EventSourceType == filters.EventSourceType &&
                        cached.Filters.EventStreamType == filters.EventStreamType &&
                        cached.Filters.Tags.SequenceEqual(filters.Tags) &&
                        cached.SubscriptionEventTypes.SequenceEqual(subscription.EventTypes) &&
                        cached.ObserverEventTypeIds.SequenceEqual(observer.EventTypes.Select(_ => _.Id)))
                    {
                        lastMatchingEvent = cached.LastMatchingEvent;
                    }
                    else
                    {
                        lastMatchingEvent = await GetLastMatchingEvent(request, subscription, lastHandled, target, filters, hasFailedPartitions, context.CancellationToken);
                        if (lastMatchingEvent is not null)
                        {
                            lastMatchingEvents[observer.Id] = new(
                                filters with { Tags = filters.Tags.ToArray() },
                                subscription.EventTypes.ToArray(),
                                observer.EventTypes.Select(_ => _.Id).ToArray(),
                                hasFailedPartitions,
                                lastMatchingEvent);
                        }
                    }
                }

                if (lastMatchingEvent is null)
                {
                    continue;
                }

                matchingObserverIds.Add(observer.Id);
                if (!hasFailedPartitions && (!lastHandled.IsActualValue || lastHandled < lastMatchingEvent))
                {
                    outstanding.Add(observer.Id);
                }
            }

            var relevantFailures = failedPartitions.Partitions.Where(_ => matchingObserverIds.Contains(_.ObserverId.Value)).ToArray();
            if (outstanding.Count == 0)
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = relevantFailures.Length == 0,
                    FailedPartitions = relevantFailures.ToContract().ToArray()
                };
            }

            if (request.TimeoutMilliseconds > 0 && stopwatch.ElapsedMilliseconds >= request.TimeoutMilliseconds)
            {
                return new WaitForObserverCompletionResponse
                {
                    TimedOut = true,
                    FailedPartitions = relevantFailures.ToContract().ToArray(),
                    OutstandingObservers = outstanding.ToArray()
                };
            }

            await Task.Delay(ObserverCompletionPollingDelayMs, context.CancellationToken);
        }
    }

    /// <inheritdoc/>
    public Task ClearObserverQuarantine(ClearObserverQuarantine command, CallContext context = default) =>
        grainFactory.GetObserver(command).ClearObserverQuarantine();

    /// <inheritdoc/>
    public Task ClearFailedPartitions(ClearFailedPartitions command, CallContext context = default) =>
        grainFactory.GetObserver(command).ClearFailedPartitions();

    /// <inheritdoc/>
    public async Task<RemoveObserverResponse> RemoveObserver(RemoveObserver command, CallContext context = default)
    {
        var eventSequenceId = string.IsNullOrEmpty(command.EventSequenceId)
            ? Concepts.EventSequences.EventSequenceId.Log
            : (Concepts.EventSequences.EventSequenceId)command.EventSequenceId;

        var result = await observerRemover.Remove(
            (Concepts.EventStoreName)command.EventStore,
            (Concepts.Observation.ObserverId)command.ObserverId,
            eventSequenceId);

        return new RemoveObserverResponse
        {
            Outcome = (ObserverRemovalOutcome)(int)result.Outcome,
            BlockingNamespace = result.Outcome == Concepts.Observation.ObserverRemovalOutcome.Removed ? string.Empty : result.BlockingNamespace.Value
        };
    }

    /// <inheritdoc/>
    public async Task<ObserverInformation> GetObserverInformation(GetObserverInformationRequest request, CallContext context = default)
    {
        var observer = grainFactory.GetObserver(request);
        var definition = await observer.GetDefinition();
        var state = await observer.GetState();
        var subscribed = await observer.IsSubscribed();
        return new ObserverInformation
        {
            Id = request.ObserverId,
            EventSequenceId = definition.EventSequenceId,
            Type = definition.Type.ToContract(),
            Owner = definition.Owner.ToContract(),
            EventTypes = definition.EventTypes.ToContract(),
            NextEventSequenceNumber = state.NextEventSequenceNumber,
            LastHandledEventSequenceNumber = state.LastHandledEventSequenceNumber,
            TailEventSequenceNumber = state.TailEventSequenceNumber,
            HandledEventCount = state.HandledEventCount,
            RunningState = state.RunningState.ToContract(),
            IsSubscribed = subscribed,
            IsReplayable = definition.IsReplayable,
            IsReplayableValue = definition.IsReplayable
        };
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Clients.ConnectedClient>> GetConnectedClientsForObserver(GetConnectedClientsForObserverRequest request, CallContext context = default)
    {
        var subscription = await grainFactory.GetObserver(request).GetSubscription();
        var clients = new List<Contracts.Clients.ConnectedClient>();
        foreach (var target in subscription.Targets.Where(_ => _.ConnectedClient is not null))
        {
            // The target holds the client as it looked when it subscribed - resolve it from the
            // silo's connected-clients registry for a fresh LastSeen, falling back to the snapshot
            // if it disconnected between the subscription being read and the lookup.
            var client = target.ConnectedClient!;
            var connectedClients = grainFactory.GetConnectedClients(target.SiloAddress);
            if (await connectedClients.IsConnected(client.ConnectionId))
            {
                client = await connectedClients.GetConnectedClient(client.ConnectionId);
            }

            clients.Add(new()
            {
                ConnectionId = client.ConnectionId,
                Version = client.Version,
                LastSeen = client.LastSeen,
                IsRunningWithDebugger = client.IsRunningWithDebugger,
                SiloAddress = target.SiloAddress.ToParsableString(),
                ProcessId = client.ProcessId,
                ProcessPath = client.ProcessPath,
                MachineName = client.MachineName,
                ClientType = client.ClientType
            });
        }

        return clients;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetObservers(AllObserversRequest request, CallContext context = default)
    {
        var observerDefinitions = await storage.GetEventStore(request.EventStore).Observers.GetAll();
        var observerStates = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Observers.GetAll();
        var observers =
            from definition in observerDefinitions
            join state in observerStates on definition.Identifier equals state.Identifier into stateGroup
            from state in stateGroup.DefaultIfEmpty(Storage.Observation.ObserverState.Empty)
            select (definition, state);

        return observers.ToContract();
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<ObserverInformation>> ObserveObservers(AllObserversRequest request, CallContext context = default)
    {
        return storage
            .GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace).Observers
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .SelectMany(async observerStates =>
            {
                // TODO: We will be formalizing these things in Grains, until then this is less than optimal.
                var observerDefinitions = await storage.GetEventStore(request.EventStore).Observers.GetAll();
                var observers =
                    from definition in observerDefinitions
                    join state in observerStates on definition.Identifier equals state.Identifier into stateGroup
                    from state in stateGroup.DefaultIfEmpty(Storage.Observation.ObserverState.Empty)
                    select (definition, state);

                return observers.ToContract();
            });
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetReplayableObserversForEventTypes(GetReplayableObserversForEventTypesRequest request, CallContext context = default)
    {
        var eventTypes = request.EventTypes.ToChronicle();
        var observerDefinitions = await storage.GetEventStore(request.EventStore).Observers.GetReplayableObserversForEventTypes(eventTypes);
        var observerStates = await storage.GetEventStore(request.EventStore).GetNamespace(request.Namespace).Observers.GetAll();

        // Inner join on purpose: only replayable observers that already have state are candidates for
        // replay. Unlike the all-observers listing — where an observer should appear even before it has
        // run — an observer with no state has nothing to replay and must not be returned here.
        var observers =
            from definition in observerDefinitions
            join state in observerStates on definition.Identifier equals state.Identifier
            select (definition, state);

        return observers.ToContract();
    }

    static bool NeedsSubscription(
        ObserverInformation observer,
        Dictionary<string, ulong> eventTypeTails,
        ulong tailEventSequenceNumber,
        HashSet<string> failedObserverIds)
    {
        if (failedObserverIds.Contains(observer.Id))
        {
            return true;
        }

        var lastHandled = (EventSequenceNumber)observer.LastHandledEventSequenceNumber;
        if (!lastHandled.IsActualValue)
        {
            return true;
        }

        var rawTarget = eventTypeTails.Count == 0 || !observer.EventTypes.Any()
            ? tailEventSequenceNumber
            : observer.EventTypes.Where(type => eventTypeTails.ContainsKey(type.Id)).Max(type => eventTypeTails[type.Id]);
        return lastHandled < (EventSequenceNumber)rawTarget;
    }

    static bool HasEffectiveFilters(Concepts.Observation.ObserverFilters filters) =>
        filters.Tags.Any() ||
        (filters.EventSourceType is { } eventSourceType && eventSourceType != EventSourceType.Unspecified) ||
        filters.EventStreamType is { IsAll: false };

    /// <summary>
    /// Check whether the appended range is small and unambiguous enough to inspect. Tail zero is
    /// safe without a presence bit because the range can contain only the first event.
    /// </summary>
    /// <param name="request">The append wait request.</param>
    /// <param name="target">The last relevant event for this observer.</param>
    /// <returns>True when it is safe to inspect the appended range.</returns>
    static bool HasBoundedAppendRange(WaitForObserverCompletionRequest request, EventSequenceNumber target) =>
        (request.HasFirstEventSequenceNumber || request.FirstEventSequenceNumber > 0 || request.TailEventSequenceNumber == EventSequenceNumber.First.Value) &&
        request.FirstEventSequenceNumber <= request.TailEventSequenceNumber &&
        request.FirstEventSequenceNumber <= target.Value &&
        request.TailEventSequenceNumber - request.FirstEventSequenceNumber < MaximumObserverCompletionRange;

    async Task<EventSequenceNumber?> GetLastMatchingEvent(
        WaitForObserverCompletionRequest request,
        Cratis.Chronicle.Observation.ObserverSubscription subscription,
        EventSequenceNumber lastHandled,
        EventSequenceNumber target,
        Concepts.Observation.ObserverFilters filters,
        bool includeHandledEvents,
        CancellationToken cancellationToken)
    {
        // The caller only scans a known appended range. A failed observer must check the entire
        // append even if its last-handled position has already reached the tail.
        var start = (EventSequenceNumber)request.FirstEventSequenceNumber;
        if (!includeHandledEvents && lastHandled.IsActualValue && lastHandled.Next() > start)
        {
            start = lastHandled.Next();
        }

        if (start > target)
        {
            return null;
        }

        var eventTypes = subscription.EventTypes.Any() ? subscription.EventTypes.ToArray() : null;
        var tags = filters.Tags.Any() ? filters.Tags.Select(_ => (Tag)_).ToArray() : null;
        var eventSequence = storage.GetEventStore(request.EventStore)
            .GetNamespace(request.Namespace)
            .GetEventSequence(request.EventSequenceId);
        using var cursor = await eventSequence.GetRange(
            start,
            target,
            eventSourceId: null,
            eventTypes: eventTypes,
            tags: tags,
            eventSourceType: filters.EventSourceType,
            eventStreamType: filters.EventStreamType,
            cancellationToken: cancellationToken);
        EventSequenceNumber? lastMatch = null;
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (eventTypes?.Any(type => type.Id == @event.Context.EventType.Id) != false &&
                    filters.Matches(@event) &&
                    (lastMatch is null || @event.Context.SequenceNumber > lastMatch))
                {
                    lastMatch = @event.Context.SequenceNumber;
                }
            }
        }

        return lastMatch;
    }

    sealed record CachedMatchingEvent(
        Concepts.Observation.ObserverFilters Filters,
        Concepts.Events.EventType[] SubscriptionEventTypes,
        string[] ObserverEventTypeIds,
        bool IncludesHandledEvents,
        EventSequenceNumber LastMatchingEvent);
}
