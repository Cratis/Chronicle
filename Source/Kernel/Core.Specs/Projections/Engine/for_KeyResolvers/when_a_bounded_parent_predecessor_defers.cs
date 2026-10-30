// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_a_bounded_parent_predecessor_defers : given.a_parent_key_history
{
    Key _key;

    void Establish() => _parent.GetKeyResolverFor(_parentEventType).Returns(
        _ => (_, _, @event) => @event.Context.SequenceNumber.Value == 1
            ? Task.FromResult(KeyResolverResult.Deferred(null!))
            : _resolvers.FromEventValueProvider(EventValueProviders.EventContent("rootId"))(_storage, _sink, @event));

    async Task Because()
    {
        await Append(0, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        await Append(1, _parentEventType, ParentSource, new { rootId = "deferred-root" });
        await Append(2, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(3, _parentEventType, ParentSource, new { rootId = LaterRoot });
        _key = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(2))).Key;
    }

    [Fact] void should_fetch_an_older_candidate_only_after_the_latest_candidate_defers() => _key.Value.ShouldEqual(OriginalRoot);
}
