// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_keyed_child_update;

public class and_the_parent_key_is_an_integer_on_a_separate_stream : given.a_flat_child_projection_with_keyed_events
{
    Key _result;

    void Establish() => Sink.TryFindRootKeyByChildValueInRoot("children.childId", ChildKey, Arg.Is<Key>(key => Equals(key.Value, 5)))
        .Returns(new Option<Key>(new Key("5", ArrayIndexers.NoIndexers)));

    async Task Because()
    {
        var resolver = KeyResolvers.FromParentHierarchy(
            ChildProjection,
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("childId")),
            (_, _, _) => Task.FromResult(KeyResolverResult.Resolved(new Key(5, ArrayIndexers.NoIndexers))),
            "childId");
        _result = ((ResolvedKey)await resolver(Storage, Sink, ChildMovedEvent)).Key;
    }

    [Fact] void should_preserve_the_integer_parent_identifier() => _result.Value.ShouldEqual(5);
    [Fact] void should_keep_the_child_indexer() => _result.ArrayIndexers.All.Single().Identifier.ShouldEqual(ChildKey);
    [Fact] void should_query_the_configured_parent_once() => Sink.Received(1).TryFindRootKeyByChildValueInRoot("children.childId", ChildKey, Arg.Is<Key>(key => Equals(key.Value, 5)));
    [Fact] void should_not_fall_back_to_an_unscoped_query() => Sink.DidNotReceive().TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), Arg.Any<object>());
    [Fact] void should_not_look_up_parent_events() => Storage.DidNotReceive().GetInstancesOfAnyInRange(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>());
}
