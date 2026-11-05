// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RederiveUniqueConstraintIndexes.given;

public class a_rederive_patch : Specification
{
    protected static readonly EventSequenceId _otherSequence = new("other");
    protected RederiveUniqueConstraintIndexes _patch;
    protected IGrainFactory _grainFactory;
    protected IConstraintsStorage _constraintsStorage;
    protected INamespaces _namespaces;
    protected IEventSequences _sequences;
    protected IEventStoreStorage _eventStoreStorage;
    protected IUniqueConstraintsStorage _logStorage;
    protected IUniqueConstraintsStorage _otherStorage;
    protected IJobsManager _jobsManager;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;

    void Establish()
    {
        _eventStore = "some-event-store";
        _namespace = "some-namespace";
        var storage = Substitute.For<IStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _constraintsStorage = Substitute.For<IConstraintsStorage>();
        _namespaces = Substitute.For<INamespaces>();
        _sequences = Substitute.For<IEventSequences>();
        _jobsManager = Substitute.For<IJobsManager>();
        _logStorage = Substitute.For<IUniqueConstraintsStorage>();
        _otherStorage = Substitute.For<IUniqueConstraintsStorage>();

        storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([_eventStore]));
        storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.Constraints.Returns(_constraintsStorage);
        _constraintsStorage.GetDefinitions().Returns(Task.FromResult<IEnumerable<IConstraintDefinition>>(
            [new UniqueConstraintDefinition("SomeUniqueConstraint", [new("SomeEvent", ["SomeProperty"])])]));
        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace]));
        _grainFactory.GetGrain<IEventSequences>(0, new EventSequencesKey(_eventStore, _namespace)).Returns(_sequences);
        _sequences.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([_otherSequence]));
        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _namespace)).Returns(_jobsManager);
        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Any<ReindexConstraintsRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));

        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _eventStoreStorage.GetNamespace(_namespace).Returns(namespaceStorage);
        namespaceStorage.GetUniqueConstraintsStorage(EventSequenceId.Log).Returns(_logStorage);
        namespaceStorage.GetUniqueConstraintsStorage(_otherSequence).Returns(_otherStorage);
        _logStorage.CountPendingSpellings().Returns(Task.FromResult(0));
        _otherStorage.CountPendingSpellings().Returns(Task.FromResult(0));

        _patch = new RederiveUniqueConstraintIndexes(storage, _grainFactory, Substitute.For<ILogger<RederiveUniqueConstraintIndexes>>());
    }
}
