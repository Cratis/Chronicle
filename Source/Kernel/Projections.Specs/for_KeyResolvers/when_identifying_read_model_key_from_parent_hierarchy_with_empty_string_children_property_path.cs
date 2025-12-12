// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_KeyResolvers;

public class when_identifying_read_model_key_from_parent_hierarchy_with_empty_string_children_property_path : Specification
{
    AppendedEvent _childEvent;
    Key _result;
    IProjection _parentProjection;
    IProjection _childProjection;
    IEventSequenceStorage _storage;
    ISink _sink;
    KeyResolvers _keyResolvers;

    static readonly EventType _parentEventType = new("5f4f4368-6989-4d9d-a84e-7393e0b41cfd", 1);
    static readonly EventType _childEventType = new("02405794-91e7-4e4f-8ad1-f043070ca297", 1);
    const string RootKey = "root-key-123";
    const string ParentKey = "parent-key-456";
    const string ChildKey = "child-key-789";
    const string ChildEventSourceId = "different-event-source-id";

    PropertyPath _queriedChildPropertyPath;

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

        _childEvent = new(
            new(
                _childEventType,
                EventSourceType.Default,
                ChildEventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                1,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System),
            new
            {
                parentId = ParentKey,
                childId = ChildKey,
                name = "Test Child"
            }.AsExpandoObject());

        _parentProjection = Substitute.For<IProjection>();
        _parentProjection.EventTypes.Returns([_parentEventType]);
        _parentProjection.OwnEventTypes.Returns([_parentEventType]);
        _parentProjection.IdentifiedByProperty.Returns((PropertyPath)"id");
        _parentProjection.Path.Returns((ProjectionPath)"configurations");

        // Set ChildrenPropertyPath to empty string instead of NotSet
        _parentProjection.ChildrenPropertyPath.Returns((PropertyPath)string.Empty);
        _parentProjection.HasParent.Returns(false);
        _parentProjection.Parent.Returns((IProjection)null!);

        _childProjection = Substitute.For<IProjection>();
        _childProjection.HasParent.Returns(true);
        _childProjection.Parent.Returns(_parentProjection);
        _childProjection.ChildrenPropertyPath.Returns((PropertyPath)"hubs");
        _childProjection.IdentifiedByProperty.Returns((PropertyPath)"hubId");
        _childProjection.Path.Returns((ProjectionPath)"configurations -> hubs");

        _storage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();

        // Parent event is NOT found by EventSourceId (returns empty)
        // This forces the code to use sink lookup
        _storage.TryGetLastInstanceOfAny(ParentKey, Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Option<AppendedEvent>.None());

        // Sink lookup returns the root key
        // Capture the property path used in the query to verify it's correct
        _sink.When(x => x.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ParentKey))
            .Do(callInfo => _queriedChildPropertyPath = callInfo.ArgAt<PropertyPath>(0));

        _sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ParentKey)
            .Returns(new Option<Key>(new Key(RootKey, ArrayIndexers.NoIndexers)));

        _sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), RootKey)
            .Returns(Option<Key>.None());
    }

    async Task Because()
    {
        try
        {
            var keyResult = await _keyResolvers.FromParentHierarchy(
                _childProjection,
                _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("childId")),
                _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
                "hubId")(_storage, _sink, _childEvent);
            _result = (keyResult as ResolvedKey)?.Key;
        }
        catch
        {
            // Ignore exceptions - we're only testing that the sink was called with the correct path
        }
    }

    [Fact] void should_treat_empty_string_as_not_set_and_query_sink_with_child_projection_children_property_path() =>
        _queriedChildPropertyPath.ShouldEqual((PropertyPath)"hubs.id");

    [Fact] void should_not_incorrectly_use_parent_empty_string_as_prefix() =>
        _queriedChildPropertyPath.ShouldNotEqual((PropertyPath)".id");
}
