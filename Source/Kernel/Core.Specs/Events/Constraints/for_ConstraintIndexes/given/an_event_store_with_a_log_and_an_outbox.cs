// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintIndexes.given;

/// <summary>
/// An event store with one namespace holding the event log and the outbox. No event sequence grain is involved: the
/// indexes are rebuilt from what the namespace holds, not from what any active grain knows.
/// </summary>
public class an_event_store_with_a_log_and_an_outbox : Specification
{
    protected ConstraintIndexes _constraintIndexes;
    protected IGrainFactory _grainFactory;
    protected IStorage _storage;
    protected IEventStoreNamespaceStorage _namespaceStorage;
    protected INamespaces _namespaces;
    protected IEventSequences _eventSequences;
    protected IJobsManager _jobsManager;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;
    protected List<ReindexConstraintsRequest> _startedReindexes;

    void Establish()
    {
        _eventStore = "some-event-store";
        _namespace = "some-namespace";
        _startedReindexes = [];

        _grainFactory = Substitute.For<IGrainFactory>();
        _storage = Substitute.For<IStorage>();
        _namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _namespaces = Substitute.For<INamespaces>();
        _eventSequences = Substitute.For<IEventSequences>();
        _jobsManager = Substitute.For<IJobsManager>();

        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace(_namespace).Returns(_namespaceStorage);
        _namespaceStorage.HasData().Returns(true);

        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace]));

        _grainFactory.GetGrain<IEventSequences>(0, new EventSequencesKey(_eventStore, _namespace)).Returns(_eventSequences);
        _eventSequences.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([EventSequenceId.Log, EventSequenceId.Outbox]));

        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _namespace)).Returns(_jobsManager);
        _jobsManager
            .Start<IReindexConstraints, ReindexConstraintsRequest>(Arg.Do<ReindexConstraintsRequest>(_startedReindexes.Add))
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Success(JobId.New())));

        _constraintIndexes = new ConstraintIndexes(_grainFactory, _storage, Substitute.For<ILogger<ConstraintIndexes>>());
    }

    protected static UniqueConstraintDefinition UniqueEmail(params EventSequenceId[] eventSequences) =>
        new("UniqueEmail", [new UniqueConstraintEventDefinition("InvitationSent", ["email"])])
        {
            EventSequences = eventSequences
        };
}
