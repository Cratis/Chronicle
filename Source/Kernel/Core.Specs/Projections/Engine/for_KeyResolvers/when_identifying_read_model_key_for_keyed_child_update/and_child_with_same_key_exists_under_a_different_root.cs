// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_keyed_child_update;

/// <summary>
/// The same child key can exist under multiple roots — the child key alone never decides the
/// placement. When the sink locates the child under a root that does not match the resolved parent
/// key, the direct resolution must be rejected and the event-sequence strategies must decide, so the
/// update lands under the parent the event belongs to and not under whichever root the sink returned.
/// </summary>
public class and_child_with_same_key_exists_under_a_different_root : given.a_flat_child_projection_with_keyed_events
{
    const string OtherRootKey = "other-root-key";

    KeyResolverResult _result;

    void Establish()
    {
        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ChildKey)
            .Returns(new Option<Key>(new Key(OtherRootKey, ArrayIndexers.NoIndexers)));

        Storage.TryGetLastInstanceOfAny(BoardKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.Contains(RootCreatedEventType.Id)))
            .Returns(new Option<AppendedEvent>(RootCreatedEvent));
    }

    async Task Because() => _result = await CreateResolverUnderTest()(Storage, Sink, ChildMovedEvent);

    [Fact] void should_resolve_to_the_board_key() => (_result as ResolvedKey).Key.Value.ShouldEqual(BoardKey);
    [Fact] void should_not_resolve_to_the_other_root() => (_result as ResolvedKey).Key.Value.ShouldNotEqual(OtherRootKey);
    [Fact] void should_fall_back_to_the_event_sequence() => Storage.Received(1).TryGetLastInstanceOfAny(BoardKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.Contains(RootCreatedEventType.Id)));
}
