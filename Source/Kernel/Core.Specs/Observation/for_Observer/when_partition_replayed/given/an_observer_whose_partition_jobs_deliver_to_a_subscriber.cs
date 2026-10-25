// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.Utilities;

namespace Cratis.Chronicle.Observation.for_Observer.when_partition_replayed.given;

public class an_observer_whose_partition_jobs_deliver_to_a_subscriber : for_Observer.given.an_observer_with_subscription_for_specific_event_type
{
    protected static readonly EventType selected_type = new("selected-replay-type", EventTypeGeneration.First);
    protected readonly Key _partition = "some-partition";
    protected readonly List<(Key Partition, AppendedEvent Event)> _deliveries = [];
    protected EventSequenceNumber _heldBackFrom = 45UL;
    protected EventSequenceNumber _liveSequenceNumber = 45UL;
    protected bool _legacyRequest;
    protected bool _deliveredWhileHeld;
    protected bool _heldBeforeCompletion;
    protected EventSequenceNumber _replayHandled;
    protected EventSequenceNumber _replayScanned;
    protected (Key Partition, AppendedEvent Event)[] _replayDeliveries;
    TestKitSilo _jobSilo;
    Func<TestKitSilo> _createJobSilo;
    readonly JsonSerializerOptions _jsonOptions = new();
    ReplayObserverPartitionRequest _replayRequest;
    CatchUpObserverPartitionRequest _catchupRequest;
    IStorage<JobStateWithLastHandledEvent> _replayState;
    TestableReplayObserverPartition _replay;
    partition_step_delivery _steps;

    async Task Establish()
    {
        subscription = subscription with { EventTypes = [event_type, selected_type] };
        _observer.SetSubscription(subscription);
        _definitionStorage.State = _definitionStorage.State with { EventTypes = [event_type, selected_type] };
        var observerProbe = ForwardObserverCalls();
        _createJobSilo = () => CreateJobSilo(observerProbe);
        _jobSilo = _createJobSilo();
        _steps = new(_createJobSilo, observerProbe, _subscriber, subscription);
        _eventSequenceStorage.GetRange(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IEnumerable<Tag>?>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(_steps.Read(call.ArgAt<EventSequenceNumber>(0), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSourceId>(2), call.ArgAt<IEnumerable<EventType>>(3))));
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(call =>
            {
                var events = call.Arg<IEnumerable<AppendedEvent>>().ToArray();
                _deliveries.AddRange(events.Select(_ => (call.Arg<Key>(), _)));
                return Task.FromResult(ObserverSubscriberResult.Ok(events[^1].Context.SequenceNumber));
            });
        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>()).Returns(call =>
            Task.FromResult<IEnumerable<EventTypeSchema>>(call.Arg<IEnumerable<EventType>>().Select(_ => new EventTypeSchema(_, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()))));
        _eventCompliance.Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        _eventGenerationRelease.Release(Arg.Any<EventStoreName>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(call => Task.FromResult(call.Arg<IEnumerable<AppendedEvent>>().ToArray()));
        CaptureJobRequests();
        _replayState = _jobSilo.StorageManager.GetStorage<JobStateWithLastHandledEvent>(typeof(TestableReplayObserverPartition).FullName!);
        _replay = await _jobSilo.CreateGrainAsync<TestableReplayObserverPartition>(JobId.New(), new JobKey(EventStoreName.NotSet, EventStoreNamespaceName.NotSet));
    }

    protected void StoreSelectedEvent(EventSequenceNumber sequenceNumber) => _steps.Store(Event(selected_type, sequenceNumber));

    protected async Task ReplayThenDeliverHeldBackEvents()
    {
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = EventSequenceNumber.Unavailable,
            NextEventSequenceNumber = _heldBackFrom
        };
        var liveEvent = Event(event_type, _liveSequenceNumber);
        _steps.Store(liveEvent);
        await _observer.ReplayPartition(_partition, [selected_type]);
        await _observer.Handle(_partition, [liveEvent]);
        _deliveredWhileHeld = _deliveries.Count > 0;
        _heldBeforeCompletion = _stateStorage.State.ReplayingPartitions.Contains(_partition);

        // Feed only real step results into replay completion; an empty cursor reports no handled watermark.
        var request = _legacyRequest ? _replayRequest with { HeldBackFrom = EventSequenceNumber.Unavailable } : _replayRequest;
        _replayState.State.Request = request;
        var replaySteps = await _replay.PrepareStepsForTesting(request);
        await _replay.StartPreparedStepsForTesting();
        _replayState.State.HandleResult(await _steps.Perform(replaySteps.Single()), _jsonOptions);
        _replayHandled = _replayState.State.LastHandledEventSequenceNumber;
        _replayScanned = _replayState.State.LastScannedEventSequenceNumber;
        _replayDeliveries = _deliveries.ToArray();
        _deliveries.Clear();

        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
            .Returns(_liveSequenceNumber);
        await _replay.CompleteForTesting();
        _catchupRequest.ShouldNotBeNull();
        var catchup = await _createJobSilo().CreateGrainAsync<TestableCatchUpObserverPartition>(JobId.New(), new JobKey(EventStoreName.NotSet, EventStoreNamespaceName.NotSet));
        var catchupSteps = await catchup.PrepareStepsForTesting(_catchupRequest);
        await _steps.Perform(catchupSteps.Single());
    }

    protected void CheckHeldBackDelivery() => _deliveries.Count(_ => _.Partition == _partition && _.Event.Context.EventSourceId == _partition.ToString() &&
        _.Event.Context.EventType == event_type && _.Event.Context.SequenceNumber == _liveSequenceNumber).ShouldEqual(1);

    IObserver ForwardObserverCalls()
    {
        var probe = Substitute.For<IObserver>();
        probe.GetSubscription().Returns(_ => _observer.GetSubscription());
        probe.GetEventTypes().Returns(_ => _observer.GetEventTypes());
        probe.PartitionReplayed(Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventType[]>())
            .Returns(call => _observer.PartitionReplayed(call.ArgAt<Key>(0), call.ArgAt<EventSequenceNumber>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<EventType[]>(3)));
        return probe;
    }

    TestKitSilo CreateJobSilo(IObserver observerProbe)
    {
        var silo = new TestKitSilo();
        var jobTypes = Substitute.For<IJobTypes>();
        jobTypes.GetFor(Arg.Any<Type>()).Returns(call => Result<JobType, IJobTypes.GetForError>.Success(new JobType(call.Arg<Type>().Name)));
        _eventStoreNamespaceStorage.JobSteps.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(ImmutableList<JobStepState>.Empty)));
        silo.AddService(jobTypes);
        silo.AddService(NullLogger<IJob>.Instance);
        silo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);
        silo.AddService(_storage);
        silo.AddService(_observerServiceClient);
        silo.AddService(_loggerFactory);
        silo.AddService(_jsonOptions);
        silo.AddService(_eventCompliance);
        silo.AddService(_eventGenerationRelease);
        silo.AddService(_configurationProvider);
        silo.AddService<TimeProvider>(TimeProvider.System);
        silo.AddService(Substitute.For<IJobStepThrottle>());
        silo.AddService<IObserverSubscriberSelector>(new RoundRobinObserverSubscriberSelector());
        silo.AddService(NullLogger<ReplayObserverPartition>.Instance);
        silo.AddService(NullLogger<CatchUpObserverPartition>.Instance);
        silo.AddService(NullLogger<HandleEventsForPartition>.Instance);
        silo.AddPersistentStateStorage<HandleEventsForPartitionState>(nameof(JobStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps);
        silo.AddProbe(_ => observerProbe);
        silo.AddProbe(_ => _subscriber);
        return silo;
    }

    void CaptureJobRequests()
    {
        _jobsManager.Start<IReplayObserverPartition, ReplayObserverPartitionRequest>(Arg.Any<ReplayObserverPartitionRequest>())
            .Returns(call =>
            {
                _replayRequest = call.Arg<ReplayObserverPartitionRequest>();
                return Task.FromResult<Result<JobId, StartJobError>>(JobId.New());
            });
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(call =>
            {
                _catchupRequest = call.Arg<CatchUpObserverPartitionRequest>();
                return Task.FromResult<Result<JobId, StartJobError>>(JobId.New());
            });
    }

    AppendedEvent Event(EventType type, EventSequenceNumber sequenceNumber) => AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(type, sequenceNumber) with
    {
        Context = EventContext.Empty with { EventType = type, SequenceNumber = sequenceNumber, EventSourceId = _partition.ToString() }
    };
}
