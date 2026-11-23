// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_a_child_targets_a_recursive_parent : given.a_parent_key_history
{
    static readonly EventType _recursiveEventType = new("recursive-parent-added", 1);
    Key _key;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""{"title":"Feature","type":"object","properties":{"id":{"type":"string"}}}""");
        var recursive = Substitute.For<IProjection>();
        _parent.HasParent.Returns(true);
        _parent.TargetReadModelSchema.Returns(schema);
        _parent.ChildrenPropertyPath.Returns((PropertyPath)"features");
        _parent.IdentifiedByProperty.Returns((PropertyPath)"id");
        _parent.ChildProjections.Returns([recursive]);
        recursive.TargetReadModelSchema.Returns(JsonSchema.FromJson(schema.ToJson()));
        recursive.OwnEventTypes.Returns([_recursiveEventType]);
        recursive.EventTypes.Returns([_recursiveEventType]);
        recursive.ChildrenPropertyPath.Returns((PropertyPath)"features.subFeatures");
        recursive.GetKeyResolverFor(_recursiveEventType).Returns((_, _, _) => Task.FromResult<KeyResolverResult>(
            new ResolvedKey(new Key(OriginalRoot, new ArrayIndexers(
                [new("features", "id", "feature"), new("features.subFeatures", "id", ParentSource)])))));
        _child.ChildrenPropertyPath.Returns((PropertyPath)"features.slices");
    }

    async Task Because()
    {
        await Append(0, _recursiveEventType, ParentSource, new { featureId = "feature" });
        await Append(1, _childEventType, ChildSource, new { parentId = ParentSource });
        _key = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(1))).Key;
    }

    [Fact] void should_resolve_the_root_through_the_recursive_event_owner() => _key.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_preserve_the_recursive_parent_indexers() => _key.ArrayIndexers.Count.ShouldEqual(3);
    [Fact] void should_locate_the_child_below_the_recursive_parent() =>
        _key.ArrayIndexers.GetFor("features.subFeatures.slices").Identifier.ShouldEqual(ChildSource);
}
