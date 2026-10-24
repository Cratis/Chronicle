// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.Utilities;

namespace Cratis.Chronicle.Observation.Jobs.for_CatchUpObserverPartition;

public class when_the_first_batch_is_fenced : Specification
{
    readonly TestKitSilo _silo = new();
    TestableCatchUpObserverPartition _job;
    IObserver _observer;
    IStorage<JobStateWithLastHandledEvent> _stateStorage;

    async Task Establish()
    {
        _observer = Substitute.For<IObserver>();
        var jobStorage = Substitute.For<IJobStorage>();
        var jobStepStorage = Substitute.For<IJobStepStorage>();
        var jobTypes = Substitute.For<IJobTypes>();
        var storage = Substitute.For<Cratis.Chronicle.Storage.IStorage>();
        var eventStore = Substitute.For<Cratis.Chronicle.Storage.IEventStoreStorage>();
        var eventStoreNamespace = Substitute.For<Cratis.Chronicle.Storage.IEventStoreNamespaceStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(eventStoreNamespace);
        eventStoreNamespace.Jobs.Returns(jobStorage);
        eventStoreNamespace.JobSteps.Returns(jobStepStorage);
        jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(ImmutableList<JobStepState>.Empty)));
        jobTypes.GetFor(Arg.Any<Type>())
            .Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("CatchUpObserverPartition")));

        _silo.AddService(new JsonSerializerOptions());
        _silo.AddService(storage);
        _silo.AddService(jobTypes);
        _silo.AddService(NullLogger<IJob>.Instance);
        _silo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(NullLogger.Instance);
        _silo.AddProbe(_ => _observer);

        _stateStorage = _silo.StorageManager.GetStorage<JobStateWithLastHandledEvent>(typeof(TestableCatchUpObserverPartition).FullName!);
        _job = await _silo.CreateGrainAsync<TestableCatchUpObserverPartition>(JobId.New().Value, new JobKey("event-store", "namespace"));
        await _job.Start(new CatchUpObserverPartitionRequest(
            new ObserverKey("observer-id", "event-store", "namespace", EventSequenceId.Log),
            ObserverType.Projection,
            (Key)"some-partition",
            43UL,
            []));
    }

    async Task Because()
    {
        await _job.RecordForTesting(JobStepResult.Succeeded(new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)
        {
            FencedByPartitionReplay = true
        }));
        await _job.CompleteForTesting();
    }

    [Fact] void should_remember_replay_ownership_in_job_state() => _stateStorage.State.FencedByPartitionReplay.ShouldBeTrue();
    [Fact] void should_not_report_a_catchup_that_can_start_a_fenced_successor() => _observer.DidNotReceiveWithAnyArgs().PartitionCaughtUp(default!, default!, default!);
}
