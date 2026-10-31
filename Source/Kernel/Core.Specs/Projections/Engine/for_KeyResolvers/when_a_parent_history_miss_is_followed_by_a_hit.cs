// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_a_parent_history_miss_is_followed_by_a_hit : given.a_parent_key_history
{
    KeyResolverResult _miss;
    Key _hit;
    KeyResolverResult _earlierChildAfterHit;

    async Task Because()
    {
        await Append(0, _childEventType, ChildSource, new { parentId = ParentSource });
        var earlierChild = await _storage.GetEventAt(0);
        _miss = await _resolveChild(_storage, _sink, earlierChild);
        await Append(1, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        await Append(2, _childEventType, ChildSource, new { parentId = ParentSource });
        _hit = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(2))).Key;
        _earlierChildAfterHit = await _resolveChild(_storage, _sink, earlierChild);
    }

    [Fact] void should_defer_the_child_with_no_preceding_parent() => _miss.ShouldBeOfExactType<DeferredKey>();
    [Fact] void should_not_cache_the_miss_for_a_later_child() => _hit.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_not_reuse_the_hit_for_an_earlier_boundary() => _earlierChildAfterHit.ShouldBeOfExactType<DeferredKey>();
}
