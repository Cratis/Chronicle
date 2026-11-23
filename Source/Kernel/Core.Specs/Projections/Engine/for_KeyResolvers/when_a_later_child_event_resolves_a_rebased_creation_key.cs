// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_a_later_child_event_resolves_a_rebased_creation_key : given.a_parent_key_history
{
    static readonly EventType _updatedEventType = new("child-updated", 1);
    Key _key;

    void Establish()
    {
        _parent.HasParent.Returns(true);
        _parent.ChildrenPropertyPath.Returns((PropertyPath)"features");
        _parent.EventTypes.Returns([_parentEventType, _childEventType]);
        _parent.GetKeyResolverFor(_childEventType).Returns((_, _, _) => Task.FromResult<KeyResolverResult>(
            new ResolvedKey(new Key(OriginalRoot, new ArrayIndexers(
                [new("features", "id", "feature"), new("features.subFeatures", "id", ParentSource),
                 new("features.subFeatures.slices", "id", ChildSource)])))));
        _child.ChildrenPropertyPath.Returns((PropertyPath)"features.slices");
        _child.OwnEventTypes.Returns([_childEventType, _updatedEventType]);
        _child.EventTypes.Returns([_childEventType, _updatedEventType]);
    }

    async Task Because()
    {
        await Append(0, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(1, _updatedEventType, ChildSource, new { parentId = ParentSource });
        _key = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(1))).Key;
    }

    [Fact] void should_preserve_the_root_key() => _key.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_preserve_the_rebased_indexers_without_adding_a_declared_indexer() => _key.ArrayIndexers.Count.ShouldEqual(3);
    [Fact] void should_not_append_a_top_level_slice_indexer() => _key.ArrayIndexers.HasFor("features.slices").ShouldBeFalse();
    [Fact] void should_keep_the_existing_nested_slice_indexer() => _key.ArrayIndexers.GetFor("features.subFeatures.slices").Identifier.ShouldEqual(ChildSource);
}
