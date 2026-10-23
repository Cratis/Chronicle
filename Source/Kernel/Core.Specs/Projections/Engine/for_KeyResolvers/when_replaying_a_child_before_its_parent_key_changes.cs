// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

using InMemoryEventSequenceStorage = Cratis.Chronicle.Storage.InMemory.EventSequences.EventSequenceStorage;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_replaying_a_child_before_its_parent_key_changes : Specification
{
    const string ParentSource = "parent-source";
    const string ChildSource = "child-source";
    const string OriginalRoot = "original-root";
    const string LaterRoot = "later-root";

    static readonly EventType _parentEventType = new("parent-key-selected", 1);
    static readonly EventType _childEventType = new("child-added", 1);

    InMemoryEventSequenceStorage _storage;
    ISink _sink;
    KeyResolver _resolveChild;
    Key _liveKey;
    Key _replayKey;

    void Establish()
    {
        _storage = new InMemoryEventSequenceStorage(
            new EventStoreName("parent-key-replay"),
            EventStoreNamespaceName.Default,
            EventSequenceId.Log,
            Substitute.For<IIdentityStorage>());
        _sink = Substitute.For<ISink>();
        var resolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        var parent = Substitute.For<IProjection>();
        parent.Path.Returns(new ProjectionPath("Root"));
        parent.EventTypes.Returns([_parentEventType]);
        parent.OwnEventTypes.Returns([_parentEventType]);
        parent.IdentifiedByProperty.Returns(PropertyPath.NotSet);
        parent.GetKeyResolverFor(_parentEventType).Returns(
            resolvers.FromEventValueProvider(EventValueProviders.EventContent("rootId")));

        var child = Substitute.For<IProjection>();
        child.Path.Returns(new ProjectionPath("Root.Children"));
        child.HasParent.Returns(true);
        child.Parent.Returns(parent);
        child.ChildrenPropertyPath.Returns((PropertyPath)"children");
        child.IdentifiedByProperty.Returns((PropertyPath)"id");
        _resolveChild = resolvers.FromParentHierarchy(
            child,
            resolvers.FromEventSourceId,
            resolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
            "id");
    }

    async Task Because()
    {
        await Append(0, _parentEventType, ParentSource, new { rootId = OriginalRoot });
        await Append(1, _childEventType, ChildSource, new { parentId = ParentSource });
        var childEvent = await _storage.GetEventAt(1);

        // Live processing sees only the prefix that has been appended so far.
        _liveKey = ((ResolvedKey)await _resolveChild(_storage, _sink, childEvent)).Key;
        await Append(2, _parentEventType, ParentSource, new { rootId = LaterRoot });

        // Replay resolves the SAME child against the complete log, just as catch-up does.
        // The real store filters by source/type and takes the highest sequence number.
        _replayKey = ((ResolvedKey)await _resolveChild(_storage, _sink, childEvent)).Key;
    }

    [Fact] void should_place_the_live_child_under_the_original_root() => _liveKey.Value.ShouldEqual(OriginalRoot);
    [Fact] void should_replay_the_child_under_the_same_root_as_live_processing() => _replayKey.Value.ShouldEqual(_liveKey.Value);
    [Fact] void should_preserve_the_child_identifier_during_replay() => _replayKey.ArrayIndexers.All.Single().Identifier.ShouldEqual(ChildSource);

    async Task Append(EventSequenceNumber sequenceNumber, EventType eventType, EventSourceId source, object content)
    {
        var result = await _storage.AppendMany(
        [
            new EventToAppendToStorage(
                sequenceNumber,
                EventSourceType.Default,
                source,
                EventStreamType.All,
                EventStreamId.Default,
                eventType,
                CorrelationId.New(),
                [],
                [],
                [],
                DateTimeOffset.UnixEpoch,
                content.AsExpandoObject(),
                EventHash.NotSet)
        ]);
        result.TryGetResult(out _).ShouldBeTrue();
    }
}
