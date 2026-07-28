// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Patches;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Core.Specs.Patches.for_RebuildConstraintIndexesForDecryptedValues.given;

public class a_rebuild_constraint_indexes_for_decrypted_values_patch : Specification
{
    protected RebuildConstraintIndexesForDecryptedValues _patch;
    protected IStorage _storage;
    protected IGrainFactory _grainFactory;
    protected IEventStoreStorage _eventStoreStorage;
    protected IConstraintsStorage _constraintsStorage;
    protected INamespaces _namespaces;
    protected IJobsManager _jobsManager;
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
        _namespaces = Substitute.For<INamespaces>();
        _jobsManager = Substitute.For<IJobsManager>();

        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([_eventStore]));
        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.Constraints.Returns(_constraintsStorage);

        _grainFactory.GetGrain<INamespaces>(_eventStore).Returns(_namespaces);
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>([_namespace]));

        _grainFactory.GetGrain<IJobsManager>(0, new JobsManagerKey(_eventStore, _namespace)).Returns(_jobsManager);

        _patch = new RebuildConstraintIndexesForDecryptedValues(_storage, _grainFactory, Substitute.For<ILogger<RebuildConstraintIndexesForDecryptedValues>>());
    }
}
