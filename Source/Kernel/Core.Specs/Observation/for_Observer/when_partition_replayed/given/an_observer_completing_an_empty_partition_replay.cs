// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.Utilities;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.given;

public class an_observer_completing_an_empty_partition_replay : for_Observer.given.an_observer_with_subscription_for_specific_event_type
{
    protected readonly Key _partition = "some-partition";
    protected EventSequenceNumber _heldBackFrom = 45UL;
    protected bool _replaysAllEventTypes;
    protected bool _completed = true;
    readonly TestKitSilo _jobSilo = new();
    TestableReplayObserverPartition _job;
    IStorage<JobStateWithLastHandledEvent> _jobStateStorage;

    async Task Establish()
    {
        _definitionStorage.State = _definitionStorage.State with { EventTypes = [event_type] };
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 42UL,
            NextEventSequenceNumber = 45UL,
            ReplayingPartitions = new HashSet<Key>([_partition])
        };
        ConfigureJobFramework();
        ForwardReplayCompletionToObserver();
        _jobStateStorage = _jobSilo.StorageManager.GetStorage<JobStateWithLastHandledEvent>(typeof(TestableReplayObserverPartition).FullName!);
        _job = await _jobSilo.CreateGrainAsync<TestableReplayObserverPartition>(Guid.Parse("55555555-5555-5555-5555-555555555555"), new JobKey(EventStoreName.NotSet, EventStoreNamespaceName.NotSet));
    }

    void ConfigureJobFramework()
    {
        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetFor(Arg.Any<Type>()).Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("ReplayObserverPartition")));
        _eventStoreNamespaceStorage.JobSteps.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(ImmutableList<JobStepState>.Empty)));
        _jobSilo.AddService(_storage);
        _jobSilo.AddService(_observerServiceClient);
        _jobSilo.AddService(_loggerFactory);
        _jobSilo.AddService(jobTypes);
        _jobSilo.AddService(new JsonSerializerOptions());
        _jobSilo.AddService(NullLogger<IJob>.Instance);
        _jobSilo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);
    }

    void ForwardReplayCompletionToObserver()
    {
        var observer = Substitute.For<IObserver>();
        observer.GetEventTypes().Returns([event_type]);
        observer.PartitionReplayed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventType[]>())
            .Returns(call => _observer.PartitionReplayed(call.ArgAt<Key>(0), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<EventType[]>(3)));
        _jobSilo.AddProbe(_ => observer);
    }

    protected async Task CompleteEmptyReplay()
    {
        var from = _heldBackFrom.IsActualValue ? _heldBackFrom : EventSequenceNumber.First;
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(from, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>()).Returns((EventSequenceNumber)50UL);
        await _observer.Handle(_partition, [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 50UL)]);
        _jobStateStorage.State.LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable;
        _jobStateStorage.State.LastScannedEventSequenceNumber = EventSequenceNumber.Unavailable;
        _jobStateStorage.State.HandledAllEvents = _completed;
        await _job.Start(new ReplayObserverPartitionRequest(_observerKey, ObserverType.Projection, _partition, EventSequenceNumber.First, 100UL, [event_type])
        {
            HeldBackFrom = _heldBackFrom,
            ReplaysAllEventTypes = _replaysAllEventTypes
        });
        await _job.CompleteForTesting();
    }

    protected void CheckHeldBackEventIsHandedToCatchupFrom(EventSequenceNumber from) => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ =>
            _.ObserverKey == _observerKey && _.Key == _partition && _.FromSequenceNumber == from && _.EventTypes.Contains(event_type)));

    protected void CheckReplayDidNotDeliverTheHeldBackEvent() => _subscriber.DidNotReceive()
        .OnNext(_partition, Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
}
