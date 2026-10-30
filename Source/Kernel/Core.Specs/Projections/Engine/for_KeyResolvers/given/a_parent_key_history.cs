// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

using InMemoryEventSequenceStorage = Cratis.Chronicle.Storage.InMemory.EventSequences.EventSequenceStorage;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.given;

public class a_parent_key_history : Specification
{
    protected const string ParentSource = "parent-source";
    protected const string ChildSource = "child-source";
    protected const string OriginalRoot = "original-root";
    protected const string LaterRoot = "later-root";
    protected static readonly EventType _parentEventType = new("parent-key-selected", 1);
    protected static readonly EventType _childEventType = new("child-added", 1);
    protected InMemoryEventSequenceStorage _storage;
    protected ISink _sink;
    protected KeyResolver _resolveChild;
    protected IProjection _parent;
    protected IProjection _child;
    protected KeyResolvers _resolvers;

    void Establish()
    {
        _storage = new InMemoryEventSequenceStorage("parent-key-replay", EventStoreNamespaceName.Default, EventSequenceId.Log, Substitute.For<IIdentityStorage>());
        _sink = Substitute.For<ISink>();
        _resolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        _parent = Substitute.For<IProjection>();
        _parent.Path.Returns(new ProjectionPath("Root"));
        _parent.EventTypes.Returns([_parentEventType]);
        _parent.OwnEventTypes.Returns([_parentEventType]);
        _parent.IdentifiedByProperty.Returns(PropertyPath.NotSet);
        _parent.GetKeyResolverFor(_parentEventType).Returns(_resolvers.FromEventValueProvider(EventValueProviders.EventContent("rootId")));
        _child = Substitute.For<IProjection>();
        _child.Path.Returns(new ProjectionPath("Root.Children"));
        _child.HasParent.Returns(true);
        _child.Parent.Returns(_parent);
        _child.ChildrenPropertyPath.Returns((PropertyPath)"children");
        _child.IdentifiedByProperty.Returns((PropertyPath)"id");
        _resolveChild = _resolvers.FromParentHierarchy(
            _child,
            _resolvers.FromEventSourceId,
            _resolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
            "id");
    }

    protected async Task Append(EventSequenceNumber number, EventType type, EventSourceId source, object content)
    {
        var result = await _storage.AppendMany(
        [
            new EventToAppendToStorage(number, EventSourceType.Default, source, EventStreamType.All, EventStreamId.Default, type, CorrelationId.New(), [], [], [], DateTimeOffset.UnixEpoch, content.AsExpandoObject(), EventHash.NotSet)
        ]);
        result.TryGetResult(out _).ShouldBeTrue();
    }
}
