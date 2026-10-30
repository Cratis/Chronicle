// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_resolving_an_ancestor_as_of_the_original_child : given.a_parent_key_history
{
    static readonly EventType _ancestorType = new("ancestor-key-selected", 1);
    Key _key;

    void Establish()
    {
        var ancestor = Substitute.For<IProjection>();
        ancestor.Path.Returns(new ProjectionPath("Ancestor"));
        ancestor.EventTypes.Returns([_ancestorType]);
        ancestor.OwnEventTypes.Returns([_ancestorType]);
        ancestor.IdentifiedByProperty.Returns(PropertyPath.NotSet);
        var rootResolver = _resolvers.FromEventValueProvider(EventValueProviders.EventContent("rootId"));
        ancestor.GetKeyResolverFor(_ancestorType).Returns(rootResolver);
        _parent.Parent.Returns(ancestor);
        _parent.HasParent.Returns(true);
        _parent.ChildrenPropertyPath.Returns((PropertyPath)"parents");
        var parentResolver = _resolvers.FromParentHierarchy(
            _parent,
            _resolvers.FromEventSourceId,
            _resolvers.FromEventValueProvider(EventValueProviders.EventContent("ancestorId")),
            "id");
        _parent.GetKeyResolverFor(_parentEventType).Returns(parentResolver);
    }

    async Task Because()
    {
        await Append(0, _ancestorType, "ancestor", new { rootId = OriginalRoot });
        await Append(1, _parentEventType, ParentSource, new { ancestorId = "ancestor" });
        await Append(2, _ancestorType, "ancestor", new { rootId = "at-child-root" });
        await Append(3, _childEventType, ChildSource, new { parentId = ParentSource });
        await Append(4, _ancestorType, "ancestor", new { rootId = LaterRoot });
        _key = ((ResolvedKey)await _resolveChild(_storage, _sink, await _storage.GetEventAt(3))).Key;
    }

    [Fact] void should_not_rewind_the_ancestor_to_the_older_parent_event() => _key.Value.ShouldEqual("at-child-root");
    [Fact] void should_keep_the_parent_indexer() => _key.ArrayIndexers.All.Single(i => i.ArrayProperty == "parents").Identifier.ShouldEqual(ParentSource);
    [Fact] void should_keep_the_child_indexer() => _key.ArrayIndexers.All.Single(i => i.ArrayProperty == "children").Identifier.ShouldEqual(ChildSource);
}
