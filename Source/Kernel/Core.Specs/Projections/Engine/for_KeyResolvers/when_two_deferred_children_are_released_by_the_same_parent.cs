// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_two_deferred_children_are_released_by_the_same_parent : given.a_parent_key_history
{
    KeyResolverResult _firstDeferred;
    KeyResolverResult _secondDeferred;
    Key _first;
    Key _second;
    KeyResolverResult _outsideRelease;

    async Task Because()
    {
        await Append(0, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(1, _childEventType, "second-child", new { parentId = ParentSource });
        var firstChild = await _storage.GetEventAt(0);
        var secondChild = await _storage.GetEventAt(1);
        _firstDeferred = await _resolveChild(_storage, _sink, firstChild);
        _secondDeferred = await _resolveChild(_storage, _sink, secondChild);
        await Append(2, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        using (ParentKeyResolutionScope.ForDeferredRelease(2))
        {
            _first = ((ResolvedKey)await _resolveChild(_storage, _sink, firstChild)).Key;
            _second = ((ResolvedKey)await _resolveChild(_storage, _sink, secondChild)).Key;
        }
        _outsideRelease = await _resolveChild(_storage, _sink, firstChild);
    }

    [Fact] void should_initially_defer_both_children() => (_firstDeferred is DeferredKey && _secondDeferred is DeferredKey).ShouldBeTrue();
    [Fact] void should_resolve_the_first_child_at_the_releasing_parent() => _first.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_resolve_the_second_child_at_the_same_releasing_parent() => _second.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_preserve_the_first_child_identifier() => _first.ArrayIndexers.All.Single().Identifier.ShouldEqual(ChildSource);
    [Fact] void should_preserve_the_second_child_identifier() => _second.ArrayIndexers.All.Single().Identifier.ShouldEqual("second-child");
    [Fact] void should_restore_the_ordinary_strict_boundary_after_release() => _outsideRelease.ShouldBeOfExactType<DeferredKey>();
}
