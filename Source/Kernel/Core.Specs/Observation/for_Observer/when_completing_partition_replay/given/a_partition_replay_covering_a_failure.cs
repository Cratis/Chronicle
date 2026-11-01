// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.for_Observer.given;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay.given;

public class a_partition_replay_covering_a_failure : an_observer
{
    protected readonly Key _partition = "replayed-partition";
    protected readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    protected readonly faultable_completion_storage<ObserverState> _completionStorage;
    protected readonly faultable_completion_storage<FailedPartitions> _failureStorage = new(Snapshot);
    protected bool _durablyHeldAfterFault;
    protected Exception? _exception;

    public a_partition_replay_covering_a_failure()
    {
        // The real provider derives the count from durable failures on activation, not from the observer snapshot.
        _completionStorage = new(Snapshot, state => Snapshot(state) with
        {
            FailedPartitions = _failureStorage.PersistedState.Partitions.ToArray(),
            FailedPartitionCount = _failureStorage.PersistedState.Partitions.Count()
        });
        _silo.Options.StorageFactory = type =>
            type == typeof(ObserverState) ? _completionStorage : type == typeof(FailedPartitions) ? _failureStorage : null!;
    }

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 10UL,
            NextEventSequenceNumber = 60UL,
            ReplayingPartitions = new HashSet<Key>([_partition]),
            FailedPartitionCount = 1
        };
        _failedPartitionsStorage.State = new();
        _failedPartitionsStorage.State.RegisterAttempt(_partition, 12UL, ["Failure"], string.Empty);
        GivenFailedEventAt(_partition, 12UL, _eventType);
        EventSequenceHasNextEvent(50UL);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));
        await _stateStorage.WriteStateAsync();
        await _failedPartitionsStorage.WriteStateAsync();
        _completionStorage.Stats.ResetCounts();
        _failureStorage.Stats.ResetCounts();
    }

    protected override ObserverState PersistedObserverState => _completionStorage.PersistedState;

    protected override void ConfigureStorage(TestKitSilo silo, ObserverState persistedState) => silo.Options.StorageFactory = type =>
        type == typeof(ObserverState) ? _completionStorage : type == typeof(FailedPartitions) ? _failureStorage : null!;

    protected Task CompleteReplay() => _observer.PartitionReplayed(_partition, 42UL, 50UL, [_eventType]);

    protected async Task CompleteWithFault()
    {
        _exception = await Cratis.Specifications.Catch.Exception(CompleteReplay);
        var stored = _completionStorage.PersistedState;
        _durablyHeldAfterFault = stored.ReplayingPartitions.Contains(_partition) || stored.CatchingUpPartitions.Contains(_partition);
    }

    protected void CheckSuccessor() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ =>
            _.Key == _partition && _.FromSequenceNumber == (EventSequenceNumber)51UL));

    static ObserverState Snapshot(ObserverState state) => state with
    {
        ReplayingPartitions = state.ReplayingPartitions.ToHashSet(),
        CatchingUpPartitions = state.CatchingUpPartitions.ToHashSet(),
        InFlightPartitions = state.InFlightPartitions.ToHashSet()
    };

    static FailedPartitions Snapshot(FailedPartitions state) => new()
    {
        Partitions = state.Partitions.Select(partition => new FailedPartition
        {
            Id = partition.Id,
            ObserverId = partition.ObserverId,
            Partition = partition.Partition,
            IsQuarantined = partition.IsQuarantined,
            IsResolved = partition.IsResolved,
            Attempts = partition.Attempts.ToArray(),
            WebhookDisposition = partition.WebhookDisposition,
            WebhookRecoveryId = partition.WebhookRecoveryId,
            AttemptsBeforeBudgetReset = partition.AttemptsBeforeBudgetReset,
            Deferral = partition.Deferral,
            RecoveryCursor = partition.RecoveryCursor
        }).ToArray()
    };
}
