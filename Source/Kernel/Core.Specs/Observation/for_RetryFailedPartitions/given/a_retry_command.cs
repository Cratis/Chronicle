// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_RetryFailedPartitions.given;

public class a_retry_command : Specification
{
    protected readonly ObserverKey _key = new("some-observer", "some-event-store", "some-namespace", "inbox-source");
    protected readonly JobId _acceptedJob = JobId.New();
    protected IStorage _storage;
    protected IGrainFactory _grainFactory;
    protected IObserver _observer;
    protected IJobsManager _jobsManager;
    protected IFailedPartitionsStorage _failedPartitionsStorage;

    protected void HaveFailedPartitions(int count) => _failedPartitionsStorage.GetFor(Arg.Any<ObserverId>()).Returns(Task.FromResult(FailedPartitionsOf(count)));

    static FailedPartitions FailedPartitionsOf(int count)
    {
        var failedPartitions = new FailedPartitions();
        for (var index = 0; index < count; index++)
        {
            failedPartitions.AddFailedPartition($"partition-{index}");
        }

        return failedPartitions;
    }

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        _failedPartitionsStorage = Substitute.For<IFailedPartitionsStorage>();
        _storage.GetEventStore(_key.EventStore).Returns(eventStoreStorage);
        eventStoreStorage.Observers.Returns(definitions);
        eventStoreStorage.GetNamespace(_key.Namespace).Returns(namespaceStorage);
        namespaceStorage.FailedPartitions.Returns(_failedPartitionsStorage);
        definitions.Get(_key.ObserverId).Returns(new ObserverDefinition(
            _key.ObserverId, [], _key.EventSequenceId, ObserverType.Reactor, ObserverOwner.Client, true));
        _observer = Substitute.For<IObserver>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);

        _jobsManager = Substitute.For<IJobsManager>();
        _grainFactory.GetGrain<IJobsManager>(Arg.Any<long>(), Arg.Any<string>()).Returns(_jobsManager);
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _jobsManager.Start<IObserverOperatorCommandJob, ObserverOperatorCommandRequest>(Arg.Any<ObserverOperatorCommandRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(_acceptedJob)));
    }
}
