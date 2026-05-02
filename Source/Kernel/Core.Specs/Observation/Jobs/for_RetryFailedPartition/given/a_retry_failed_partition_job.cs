// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.Core;
using Orleans.TestKit;
using Orleans.Utilities;
using IChronicleStorage = Cratis.Chronicle.Storage.IStorage;
using IEventStoreNamespaceStorage = Cratis.Chronicle.Storage.IEventStoreNamespaceStorage;
using IEventStoreStorage = Cratis.Chronicle.Storage.IEventStoreStorage;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition.given;

public class a_retry_failed_partition_job : Specification
{
    protected TestKitSilo _silo = new();
    protected TestableRetryFailedPartition _job;
    protected IObserver _observer;
    protected IChronicleStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IJobStorage _jobStorage;
    protected IJobStepStorage _jobStepStorage;
    protected IJobTypes _jobTypes;
    protected JobId _jobId;
    protected JobKey _jobKey;
    protected RetryFailedPartitionRequest _request;
    protected IStorage<JobStateWithLastHandledEvent> _stateStorage;

    async Task Establish()
    {
        _jobId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        _jobKey = new("event-store", "namespace");

        var observerKey = new ObserverKey("observer-id", "event-store", "namespace", EventSequenceId.Log);
        _request = new(
            observerKey,
            ObserverType.Projection,
            (Key)"some-partition",
            EventSequenceNumber.First,
            []);

        _storage = Substitute.For<IChronicleStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _jobStorage = Substitute.For<IJobStorage>();
        _jobStepStorage = Substitute.For<IJobStepStorage>();
        _jobTypes = Substitute.For<IJobTypes>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.Jobs.Returns(_jobStorage);
        _namespaceStorage.JobSteps.Returns(_jobStepStorage);

        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>())
            .Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(ImmutableList<JobStepState>.Empty)));

        _jobTypes.GetFor(Arg.Any<Type>())
            .Returns(Result<JobType, IJobTypes.GetForError>.Success(new JobType("RetryFailedPartition")));

        _silo.AddService(new JsonSerializerOptions());
        _silo.AddService(_storage);
        _silo.AddService(_jobTypes);
        _silo.AddService(NullLogger<IJob>.Instance);
        _silo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);

        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(NullLogger.Instance);

        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);

        _stateStorage = _silo.StorageManager.GetStorage<JobStateWithLastHandledEvent>(
            typeof(TestableRetryFailedPartition).FullName!);

        _job = await _silo.CreateGrainAsync<TestableRetryFailedPartition>(_jobId, _jobKey);
    }
}
