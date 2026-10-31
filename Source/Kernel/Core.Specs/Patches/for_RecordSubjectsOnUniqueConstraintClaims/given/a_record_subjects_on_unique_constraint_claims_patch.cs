// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Orleans.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RecordSubjectsOnUniqueConstraintClaims.given;

public class a_record_subjects_on_unique_constraint_claims_patch : Specification
{
    protected RecordSubjectsOnUniqueConstraintClaims _patch;
    protected IStorage _storage;
    protected IGrainFactory _grainFactory;
    protected IEventStoreStorage _eventStoreStorage;
    protected IConstraintsStorage _constraintsStorage;
    protected IEventTypesStorage _eventTypesStorage;
    protected INamespaces _namespaces;
    protected IJobsManager _jobsManager;
    protected IEventSequences _eventSequences;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;

    void Establish()
    {
        _eventStore = "some-event-store";
        _namespace = "some-namespace";

        _storage = Substitute.For<IStorage>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _constraintsStorage = Substitute.For<IConstraintsStorage>();
        _eventTypesStorage = Substitute.For<IEventTypesStorage>();
        _namespaces = Substitute.For<INamespaces>();
        _jobsManager = Substitute.For<IJobsManager>();
        _eventSequences = Substitute.For<IEventSequences>();

        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([_eventStore]));
        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.Constraints.Returns(_constraintsStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypesStorage);

        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace]));

        _grainFactory.GetGrain<IEventSequences>(0, new EventSequencesKey(_eventStore, _namespace)).Returns(_eventSequences);
        _eventSequences.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>([EventSequenceId.Log]));

        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _namespace)).Returns(_jobsManager);

        _patch = new RecordSubjectsOnUniqueConstraintClaims(_storage, _grainFactory, Substitute.For<ILogger<RecordSubjectsOnUniqueConstraintClaims>>());
    }
}
