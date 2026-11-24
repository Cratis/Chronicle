// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_keyed_child_update;

public class and_child_has_a_typed_root_key_in_the_sink : given.a_flat_child_projection_with_keyed_events
{
    static readonly Guid _rootId = Guid.Parse("8e98db5c-893f-498c-880d-2b926b72b851");
    Key _result;

    void Establish()
    {
        ChildMovedEvent = CreateEvent(7, ChildMovedEventType, _rootId.ToString(), new { childId = ChildKey }.AsExpandoObject());
        Sink.TryFindRootKeyByChildValue("children.childId", ChildKey)
            .Returns(new Option<Key>(new Key("other-root", ArrayIndexers.NoIndexers)));
        Storage.WithParentHistory(_rootId.ToString(), CreateEvent(0, RootCreatedEventType, _rootId.ToString(), new { }.AsExpandoObject()));
        Sink.TryFindRootKeyByChildValueInRoot("children.childId", ChildKey, Arg.Is<Key>(key => Equals(key.Value, _rootId.ToString())))
            .Returns(new Option<Key>(new Key(_rootId, ArrayIndexers.NoIndexers)));
    }

    async Task Because()
    {
        var resolver = KeyResolvers.FromParentHierarchy(
            ChildProjection,
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("childId")),
            KeyResolvers.FromEventSourceId,
            "childId");
        _result = ((ResolvedKey)await resolver(Storage, Sink, ChildMovedEvent)).Key;
    }

    [Fact] void should_preserve_the_typed_root_identifier() => _result.Value.ShouldEqual(_rootId);
    [Fact] void should_keep_the_child_indexer() => _result.ArrayIndexers.All.Single().Identifier.ShouldEqual(ChildKey);
    [Fact] void should_not_look_up_parent_events() => Storage.DidNotReceive().GetInstancesOfAnyInRange(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>());
    [Fact] void should_not_scan_parent_history() => Storage.DidNotReceive().GetHeadSequenceNumber(Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>());
}
