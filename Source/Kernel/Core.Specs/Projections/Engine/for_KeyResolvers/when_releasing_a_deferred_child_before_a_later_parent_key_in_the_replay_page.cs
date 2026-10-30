// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_releasing_a_deferred_child_before_a_later_parent_key_in_the_replay_page : given.a_parent_key_history
{
    KeyResolverResult _deferred;
    Key _live;
    Key _replay;
    Key _afterKeyChange;

    async Task Because()
    {
        await Append(0, _childEventType, ChildSource, new { parentId = ParentSource });
        var child = await _storage.GetEventAt(0);
        _deferred = await _resolveChild(_storage, _sink, child);
        await Append(1, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        using (ParentKeyResolutionScope.ForDeferredRelease(1))
        {
            _live = ((ResolvedKey)await _resolveChild(_storage, _sink, child)).Key;
        }

        await Append(2, _parentEventType, ParentSource, new { rootId = LaterRoot });
        using (ParentKeyResolutionScope.ForDeferredRelease(1))
        {
            _replay = ((ResolvedKey)await _resolveChild(_storage, _sink, child)).Key;
        }

        await Append(3, _childEventType, "later-child", new { parentId = ParentSource });
        _afterKeyChange = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(3))).Key;
    }

    [Fact] void should_initially_defer_the_child() => _deferred.ShouldBeOfExactType<DeferredKey>();
    [Fact] void should_release_live_using_the_releasing_parent_key() => _live.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_ignore_the_later_parent_key_already_stored_in_the_replay_page() => _replay.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_match_live_release_during_replay() => _replay.Value.ShouldEqual(_live.Value);
    [Fact] void should_not_keep_the_release_bound_for_later_children() => _afterKeyChange.Value.ShouldEqual(LaterRoot);
}
