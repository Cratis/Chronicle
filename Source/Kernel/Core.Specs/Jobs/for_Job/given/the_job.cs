// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans.TestKit;
using Orleans.Utilities;
namespace Cratis.Chronicle.Jobs.for_Job.given;

public class the_job : Specification
{
    protected TestKitSilo _silo = new();
    protected SomeJob _job;
    protected JobId _jobId;
    protected JobKey _jobKey;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected IJobStorage _jobStorage;
    protected IJobStepStorage _jobStepStorage;

    protected List<JobStepState> _storedJobStepStates;

    async Task Establish()
    {
        _storedJobStepStates = [];
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _jobStorage = Substitute.For<IJobStorage>();
        _jobStepStorage = Substitute.For<IJobStepStorage>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);

        _namespaceStorage.JobSteps.Returns(_jobStepStorage);

        _jobStepStorage.GetForJob(Arg.Any<JobId>(), Arg.Any<JobStepStatus[]>()).Returns(Task.FromResult(Catch<IImmutableList<JobStepState>>.Success(_storedJobStepStates.ToImmutableList())));
        _silo.AddService(_storage);
        _silo.AddService(NullLogger<IJob>.Instance);
        _silo.AddService(NullLogger<ObserverManager<IJobObserver>>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        _jobId = Guid.Parse("fefd1ea0-f739-4d68-8817-6c85f722dec4");
        _jobKey = new("event-store", "namespace");
        _job = await _silo.CreateGrainAsync<SomeJob>(_jobId, _jobKey);
    }
}
