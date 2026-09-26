// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
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

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayObserverPartition.given;

public class a_partition_replay_job : Specification
{
    protected TestKitSilo _silo = new();
    protected TestableReplayObserverPartition _job;
    protected IObserver _observer;
    protected IObserverServiceClient _replayStateServiceClient;
    protected ReplayObserverPartitionRequest _request;
    protected IStorage<JobStateWithLastHandledEvent> _stateStorage;

    async Task Establish()
    {
        _request = new(
            new ObserverKey("observer-id", "event-store", "namespace", EventSequenceId.Log),
            ObserverType.Projection,
            (Key)"some-partition",
            0UL,
            100UL,
            []) { ReplaysAllEventTypes = true };

        _replayStateServiceClient = Substitute.For<IObserverServiceClient>();
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
            .Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("ReplayObserverPartition")));

        _silo.AddService(new JsonSerializerOptions());
        _silo.AddService(storage);
        _silo.AddService(jobTypes);
        _silo.AddService(_replayStateServiceClient);
        _silo.AddService(NullLogger<IJob>.Instance);
        _silo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(NullLogger.Instance);
        _silo.AddProbe(_ => _observer);

        _stateStorage = _silo.StorageManager.GetStorage<JobStateWithLastHandledEvent>(typeof(TestableReplayObserverPartition).FullName!);
        _job = await _silo.CreateGrainAsync<TestableReplayObserverPartition>(Guid.Parse("55555555-5555-5555-5555-555555555555"), new JobKey("event-store", "namespace"));
    }
}
