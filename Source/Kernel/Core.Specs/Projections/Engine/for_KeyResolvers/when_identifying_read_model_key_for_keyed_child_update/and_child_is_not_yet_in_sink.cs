// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_keyed_child_update;

/// <summary>
/// When the child is not in the sink yet — such as when the current event is the child's creation
/// event — the resolution must fall through to the event-sequence strategies and resolve via the
/// parent's creation event as before.
/// </summary>
public class and_child_is_not_yet_in_sink : given.a_flat_child_projection_with_keyed_events
{
    KeyResolverResult _result;

    void Establish()
    {
        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ChildKey)
            .Returns(Option<Key>.None());

        Storage.TryGetLastInstanceOfAny(BoardKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.Contains(RootCreatedEventType.Id)))
            .Returns(new Option<AppendedEvent>(RootCreatedEvent));
    }

    async Task Because() => _result = await CreateResolverUnderTest()(Storage, Sink, ChildAddedEvent);

    [Fact] void should_resolve_to_the_board_key() => (_result as ResolvedKey).Key.Value.ShouldEqual(BoardKey);
    [Fact] void should_have_array_indexer_for_the_child() => (_result as ResolvedKey).Key.ArrayIndexers.All.Single(_ => _.ArrayProperty == "children").Identifier.ShouldEqual(ChildKey);
    [Fact] void should_look_up_the_parent_event_in_the_event_sequence() => Storage.Received(1).TryGetLastInstanceOfAny(BoardKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.Contains(RootCreatedEventType.Id)));
}
