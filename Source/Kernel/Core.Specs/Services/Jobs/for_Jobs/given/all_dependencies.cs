// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Services.Jobs.for_Jobs.given;

/// <summary>
/// Base context that wires up the dependencies of the Jobs gRPC service with a failing jobs storage.
/// </summary>
public class all_dependencies : Specification
{
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IJobStorage _jobStorage;
    protected IJobStepStorage _jobStepStorage;
    internal RecordingLogger<Jobs> _logger;
    protected Exception _exception;
    protected Contracts.Jobs.IJobs _service;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _jobStorage = Substitute.For<IJobStorage>();
        _jobStepStorage = Substitute.For<IJobStepStorage>();
        _logger = new RecordingLogger<Jobs>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.Jobs.Returns(_jobStorage);
        _namespaceStorage.JobSteps.Returns(_jobStepStorage);

        _exception = new InvalidOperationException("Jobs storage is unavailable");

        Catch<ISubject<IEnumerable<JobState>>> failedObserve = _exception;
        _jobStorage.ObserveJobs(Arg.Any<JobStatus[]>()).Returns(failedObserve);

        Catch<IImmutableList<JobStepState>> failedSteps = _exception;
        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(failedSteps));

        _service = new Jobs(_grainFactory, _storage, _logger);
    }
}
