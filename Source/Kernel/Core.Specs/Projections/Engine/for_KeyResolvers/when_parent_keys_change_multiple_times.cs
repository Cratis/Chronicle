// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_parent_keys_change_multiple_times : given.a_parent_key_history
{
    Key _firstKey;
    Key _secondKey;
    Key _thirdKey;

    async Task Because()
    {
        await Append(0, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        await Append(1, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(2, _parentEventType, ParentSource, new { rootId = LaterRoot });
        await Append(3, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(4, _parentEventType, ParentSource, new { rootId = "third-root" });
        await Append(5, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(6, _childEventType, ParentSource, new { rootId = "unrelated-future-root" });

        _firstKey = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(1))).Key;
        _secondKey = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(3))).Key;
        _thirdKey = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(5))).Key;
    }

    [Fact] void should_keep_the_first_child_under_the_first_root() => _firstKey.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_place_the_second_child_under_the_second_root() => _secondKey.Value.ShouldEqual(LaterRoot);
    [Fact] void should_ignore_unrelated_future_events_on_the_parent_source() => _thirdKey.Value.ShouldEqual("third-root");
}
