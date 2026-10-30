// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_the_child_event_also_creates_its_parent : given.a_parent_key_history
{
    Key _liveKey;
    Key _replayKey;

    void Establish()
    {
        _parent.EventTypes.Returns([_childEventType]);
        _parent.OwnEventTypes.Returns([_childEventType]);
        var resolve = _resolvers.FromEventValueProvider(EventValueProviders.EventContent("rootId"));
        _parent.GetKeyResolverFor(_childEventType).Returns(resolve);
    }

    async Task Because()
    {
        await Append(0, _childEventType, ChildSource, new { parentId = ParentSource, rootId = OriginalRoot });
        var childEvent = await _storage.GetEventAt(0);
        _liveKey = ((ResolvedKey)await _resolveChild(_storage, _sink, childEvent)).Key;
        await Append(1, _childEventType, ChildSource, new { parentId = ParentSource, rootId = LaterRoot });
        _replayKey = ((ResolvedKey)await _resolveChild(_storage, _sink, childEvent)).Key;
    }

    [Fact] void should_use_the_current_event_directly_even_though_stored_history_is_strictly_before() => _liveKey.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_keep_the_child_indexer() => _replayKey.ArrayIndexers.All.Single().Identifier.ShouldEqual(ChildSource);
    [Fact] void should_replay_the_same_event_under_the_same_root() => _replayKey.Value.ShouldEqual(_liveKey.Value);
}
