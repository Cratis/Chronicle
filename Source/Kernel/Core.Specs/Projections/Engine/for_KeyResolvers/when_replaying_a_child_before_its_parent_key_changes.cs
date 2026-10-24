// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_replaying_a_child_before_its_parent_key_changes : given.a_parent_key_history
{
    Key _liveKey;
    Key _laggingLiveKey;
    Key _replayKey;

    async Task Because()
    {
        await Append(0, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        await Append(1, _childEventType, ChildSource, new { parentId = ParentSource });
        var childEvent = await _storage.GetEventAt(1);
        _liveKey = ((ResolvedKey)await _resolveChild(_storage, _sink, childEvent)).Key;
        await Append(2, _parentEventType, ParentSource, new { rootId = LaterRoot });

        // A lagging live observer has the later parent fact in storage before handling the child.
        _laggingLiveKey = ((ResolvedKey)await _resolveChild(_storage, _sink, childEvent)).Key;
        _replayKey = ((ResolvedKey)await _resolveChild(_storage, _sink, childEvent)).Key;
    }

    [Fact] void should_place_the_live_child_under_the_original_root() => _liveKey.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_not_let_a_lagging_live_observer_see_the_future_parent() => _laggingLiveKey.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_replay_the_child_under_the_same_root_as_live_processing() => _replayKey.Value.ShouldEqual(_liveKey.Value);
    [Fact] void should_preserve_the_child_identifier_during_replay() => _replayKey.ArrayIndexers.All.Single().Identifier.ShouldEqual(ChildSource);
}
