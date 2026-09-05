// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Services.Events;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/>.</param>
/// <param name="storage">The <see cref="IStorage"/>.</param>
internal sealed class Observers(IGrainFactory grainFactory, IStorage storage) : IObservers
{
    const int ObserverCompletionPollingDelayMs = 50;

    /// <inheritdoc/>
    public Task RetryPartition(RetryPartition command, CallContext context = default) =>
        grainFactory.GetObserver(command).TryStartRecoverJobForFailedPartition(command.Partition);

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
                .Where(_ => _.EventSequenceId == request.EventSequenceId)
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
            if (observers.All(_ =>
                (((EventSequenceNumber)_.LastHandledEventSequenceNumber).IsActualValue &&
                 _.LastHandledEventSequenceNumber >= request.TailEventSequenceNumber) ||
                failedObserverIds.Contains(_.Id)))
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = !failedPartitions.Partitions.Any(),
                    FailedPartitions = failedPartitions.Partitions.ToContract().ToArray()
                };
            }

            await Task.Delay(ObserverCompletionPollingDelayMs, context.CancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<AppliedThroughResponse> AppliedThrough(AppliedThroughRequest request, CallContext context = default)
    {
        var requestedIds = request.ObserverIds.ToHashSet(StringComparer.Ordinal);
        var outcomes = new Dictionary<string, AppliedThroughOutcome>(StringComparer.Ordinal);

        try
        {
            while (outcomes.Count < requestedIds.Count)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var observers = (await GetObservers(
                    new AllObserversRequest
                    {
                        EventStore = request.EventStore,
                        Namespace = request.Namespace
                    },
                    context))
                    .Where(_ => _.EventSequenceId == request.EventSequenceId && requestedIds.Contains(_.Id))
                    .ToArray();

                var foundIds = observers.Select(_ => _.Id).ToHashSet(StringComparer.Ordinal);
                foreach (var missingId in requestedIds.Except(foundIds))
                {
                    outcomes[missingId] = AppliedThroughOutcome.Unavailable;
                }

                if (observers.Length > 0)
                {
                    var observerIds = observers.Select(_ => (Concepts.Observation.ObserverId)_.Id).ToArray();
                    var failedPartitions = await storage
                        .GetEventStore(request.EventStore)
                        .GetNamespace(request.Namespace)
                        .FailedPartitions
                        .GetFor(observerIds);
                    var failedObserverIds = failedPartitions.Partitions.Select(_ => _.ObserverId.Value).ToHashSet(StringComparer.Ordinal);

                    foreach (var observer in observers)
                    {
                        // A null classification means the observer is still active and simply has not reached
                        // the target yet - it stays out of `outcomes` so the loop keeps polling it, rather than
                        // being reported as a terminal outcome prematurely.
                        var outcome = ClassifyObserver(observer, failedObserverIds, request.TargetEventSequenceNumber);
                        if (outcome is not null)
                        {
                            outcomes[observer.Id] = outcome.Value;
                        }
                    }
                }

                if (outcomes.Count == requestedIds.Count)
                {
                    break;
                }

                await Task.Delay(ObserverCompletionPollingDelayMs, context.CancellationToken);
            }
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // The caller's deadline elapsed - report it as a typed outcome for whichever observers had not
            // resolved yet, rather than letting the whole call fault. Success must never be inferred from the
            // cancellation itself, only from outcomes already durably observed above.
        }

        foreach (var stillUnresolved in requestedIds.Except(outcomes.Keys))
        {
            outcomes[stillUnresolved] = AppliedThroughOutcome.TimedOut;
        }

        return new AppliedThroughResponse
        {
            IsSuccess = outcomes.Values.All(_ => _ == AppliedThroughOutcome.Ready),
            Results = requestedIds.Select(id => new AppliedThroughObserverResult { ObserverId = id, Outcome = outcomes[id] }).ToArray()
        };
    }

    /// <inheritdoc/>
    public Task ClearObserverQuarantine(ClearObserverQuarantine command, CallContext context = default) =>
        grainFactory.GetObserver(command).ClearObserverQuarantine();

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
            IsReplayable = definition.IsReplayable
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

    /// <summary>
    /// Classifies a single observer's current state against the target position.
    /// </summary>
    /// <param name="observer">The observer's current <see cref="ObserverInformation"/>.</param>
    /// <param name="failedObserverIds">The set of observer ids with a failed partition.</param>
    /// <param name="targetEventSequenceNumber">The target position to compare against.</param>
    /// <returns>The terminal outcome, or <see langword="null"/> when the observer is still active and simply has not reached the target yet.</returns>
    static AppliedThroughOutcome? ClassifyObserver(ObserverInformation observer, HashSet<string> failedObserverIds, ulong targetEventSequenceNumber)
    {
        if (failedObserverIds.Contains(observer.Id))
        {
            return AppliedThroughOutcome.Failed;
        }

        // Checked before the position, deliberately: a replaying observer's checkpoint can be rewinding, so its
        // current LastHandledEventSequenceNumber momentarily looking past the target must never be read as
        // durable forward progress - the running state is what makes that distinction trustworthy.
        switch (observer.RunningState)
        {
            case ObserverRunningState.Quarantined:
                return AppliedThroughOutcome.Quarantined;
            case ObserverRunningState.Replaying:
                return AppliedThroughOutcome.Replaying;
            case ObserverRunningState.Disconnected:
                return AppliedThroughOutcome.Unavailable;
        }

        if (((EventSequenceNumber)observer.LastHandledEventSequenceNumber).IsActualValue &&
            observer.LastHandledEventSequenceNumber >= targetEventSequenceNumber)
        {
            return AppliedThroughOutcome.Ready;
        }

        return null;
    }
}
