// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_two_namespaces_have_different_join_data : given.a_factory_for_two_namespaces
{
    IProjection _firstProjection;
    IProjection _secondProjection;
    Changeset<AppendedEvent, ExpandoObject> _firstChangeset;
    Changeset<AppendedEvent, ExpandoObject> _secondChangeset;

    async Task Because()
    {
        _firstProjection = await _factory.Create(_eventStore, _first, _definition, _readModel, []);
        _secondProjection = await _factory.Create(_eventStore, _second, _definition, _readModel, []);
        _firstChangeset = Project(_firstProjection);
        _secondChangeset = Project(_secondProjection);
    }

    [Fact] void should_join_from_the_first_namespaces_event_sequence() => NameOf(_firstChangeset).ShouldEqual("alpha");
    [Fact] void should_join_from_the_second_namespaces_event_sequence() => NameOf(_secondChangeset).ShouldEqual("beta");
    [Fact] void should_read_the_first_namespace_once() => _ = _firstStorage.Received(1).TryGetLastEventBefore(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_read_the_second_namespace_once() => _ = _secondStorage.Received(1).TryGetLastEventBefore(Arg.Any<EventTypeId>(), Arg.Any<EventSourceId>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_give_each_namespace_its_own_projection_instance() => _firstProjection.ShouldNotBeSame(_secondProjection);
    [Fact] void should_give_each_namespace_its_own_initial_state() => _firstProjection.InitialModelState.ShouldNotBeSame(_secondProjection.InitialModelState);
    [Fact] void should_give_the_same_fingerprint() => _firstProjection.DefinitionFingerprint.ShouldEqual(_secondProjection.DefinitionFingerprint);
    [Fact] void should_subscribe_to_the_same_event_types() => _firstProjection.EventTypes.ShouldContainOnly(_secondProjection.EventTypes);
}
