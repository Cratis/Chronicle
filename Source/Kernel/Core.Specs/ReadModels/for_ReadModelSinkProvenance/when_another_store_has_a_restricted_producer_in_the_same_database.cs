// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_another_store_has_a_restricted_producer_in_the_same_database : given.a_provenance_inventory
{
    Exception _error;

    void Establish()
    {
        EventStoreName other = "other";
        var store = Substitute.For<IEventStoreStorage>();
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([Store, other]));
        _storage.GetEventStore(other).Returns(store);
        store.ReadModels.GetAll().Returns(Task.FromResult<IEnumerable<ReadModelDefinition>>([_model]));
        store.Projections.GetAll().Returns(Task.FromResult<IEnumerable<ProjectionDefinition>>([_projection with { Owner = ProjectionOwner.Server }]));
        store.Namespaces.GetAll().Returns(Task.FromResult<IEnumerable<NamespaceState>>([new("another-namespace", DateTimeOffset.UnixEpoch)]));
        var scope = Substitute.For<IEventStoreNamespaceStorage>();
        scope.Sinks.GetProvenance(Arg.Any<ReadModelDefinition>()).Returns(Marker(EventStoreNamespaceName.Default));
        store.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(scope);
    }

    async Task Because() => _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));

    [Fact] void should_refuse_the_first_normal_read_before_any_payload_work() => _error.ShouldBeOfExactType<ReadModelIsNotAvailable>();
}
