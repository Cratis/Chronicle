// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_creating_for_many_namespaces_from_one_definition : given.a_factory_for_two_namespaces
{
    const int Namespaces = 50;
    IProjection[] _runs;
    Changeset<AppendedEvent, ExpandoObject> _firstChangeset;
    Changeset<AppendedEvent, ExpandoObject> _lastChangeset;
    IEventTypesStorage _inner;

    async Task Because()
    {
        // The registry is what the providers hand out, and what lets a projection tell that no event type changed without reading any schema.
        _inner = Substitute.For<IEventTypesStorage>();
        _inner.GetAllDefinitions().Returns(Task.FromResult<IEnumerable<EventTypeDefinition>>([]));
        _inner.GetLatestForAllEventTypes().Returns(Task.FromResult<IEnumerable<EventTypeSchema>>([]));
        _storage.GetEventStore(_eventStore).EventTypes.Returns(new EventTypeRegistry(_inner));

        _runs = new IProjection[Namespaces];
        for (var index = 0; index < Namespaces; index++)
        {
            _runs[index] = await _factory.Create(_eventStore, index % 2 == 0 ? _first : _second, _definition, _readModel);
        }

        _firstChangeset = Project(_runs[0]);
        _lastChangeset = Project(_runs[Namespaces - 1]);
    }

    [Fact] void should_compile_one_plan() => _factory.CachedPlanCount.ShouldEqual(1);
    [Fact] void should_read_the_event_type_schemas_once() => _ = _inner.Received(1).GetLatestForAllEventTypes();
    [Fact] void should_give_every_namespace_a_run_of_its_own() => _runs.Distinct().Count().ShouldEqual(Namespaces);
    [Fact] void should_share_the_fingerprint() => _runs.Select(_ => _.DefinitionFingerprint).Distinct().Count().ShouldEqual(1);
    [Fact] void should_join_in_the_first_namespace_with_its_own_storage() => NameOf(_firstChangeset).ShouldEqual("alpha");
    [Fact] void should_join_in_the_second_namespace_with_its_own_storage() => NameOf(_lastChangeset).ShouldEqual("beta");
}
