// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Orleans.TestKit;
namespace Cratis.Chronicle.Grains.Jobs.for_JobsManager.given;

public class the_manager : Specification
{
    protected static TestKitSilo _silo;
    protected static JobsManager _manager;
    protected static JobsManagerKey _managerKey;
    protected static IStorage _storage;
    protected static IEventStoreStorage _eventStoreStorage;
    protected static IEventStoreNamespaceStorage _namespaceStorage;
    protected static IJobStorage _jobStorage;
    protected static IJobStepStorage _jobStepStorage;

    protected static List<JobState> _storedJobs;

    async Task Establish()
    {
        _silo = new();
        _storedJobs = [];
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _jobStorage = Substitute.For<IJobStorage>();
        _jobStepStorage = Substitute.For<IJobStepStorage>();

        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(_namespaceStorage);
        _namespaceStorage.Jobs.Returns(_jobStorage);
        _namespaceStorage.JobSteps.Returns(_jobStepStorage);

        _jobStorage.GetJobs(Arg.Any<JobStatus[]>()).Returns(_ => [.. _storedJobs]);
        _jobStorage.GetJob(Arg.Any<JobId>()).Returns(callInfo => _storedJobs.Single(job => job.Id == callInfo.Arg<JobId>()));
        _jobStorage.Remove(Arg.Any<JobId>()).Returns(Task.CompletedTask);

        _jobStepStorage.RemoveAllForJob(Arg.Any<JobId>()).Returns(Task.CompletedTask);
        _silo.AddService(_storage);
        _silo.AddService(NullLogger<JobsManager>.Instance);
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _silo.AddService(loggerFactory);
        _managerKey = new ("event-store", "namespace");
        _manager = await _silo.CreateGrainAsync<JobsManager>(0, _managerKey);
    }

    protected Mock<TJob> AddJob<TJob>(JobId id)
        where TJob : class, IJob
    {
        var state = new JobState
        {
            Type = typeof(TJob),
            Id = id
        };
        _storedJobs.Add(state);
        return _silo.AddProbe<TJob>(state.Id, keyExtension: new JobKey(_managerKey.EventStore, _managerKey.Namespace));
    }
}
