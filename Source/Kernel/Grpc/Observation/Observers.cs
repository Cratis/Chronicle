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

    /// <inheritdoc/>
    public async Task<RetryPartitionResponse> RetryPartition(RetryPartition command, CallContext context = default)
    {
        var outcome = await grainFactory.GetObserver(command).TryStartRecoverJobForFailedPartition(command.Partition);
        return new RetryPartitionResponse { Outcome = (PartitionRecoveryOutcome)(int)outcome };
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
        var eventTypeIds = request.EventTypes.Select(_ => _.Id).ToHashSet(StringComparer.Ordinal);
        var stopwatch = Stopwatch.StartNew();
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
                    (eventTypeIds.Count == 0 || _.EventTypes.Any(type => eventTypeIds.Contains(type.Id))))
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
            var outstanding = observers.Where(_ =>
                !failedObserverIds.Contains(_.Id) &&
                (!((EventSequenceNumber)_.LastHandledEventSequenceNumber).IsActualValue ||
                 _.LastHandledEventSequenceNumber < request.TailEventSequenceNumber)).Select(_ => _.Id).ToArray();
            if (outstanding.Length == 0)
            {
                return new WaitForObserverCompletionResponse
                {
                    IsSuccess = !failedPartitions.Partitions.Any(),
                    FailedPartitions = failedPartitions.Partitions.ToContract().ToArray()
                };
            }

            if (request.TimeoutMilliseconds > 0 && stopwatch.ElapsedMilliseconds >= request.TimeoutMilliseconds)
            {
                return new WaitForObserverCompletionResponse
                {
                    TimedOut = true,
                    FailedPartitions = failedPartitions.Partitions.ToContract().ToArray(),
                    OutstandingObservers = outstanding
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
}
