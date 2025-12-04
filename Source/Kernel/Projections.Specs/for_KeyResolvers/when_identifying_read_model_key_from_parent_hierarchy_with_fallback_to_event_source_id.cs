// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_KeyResolvers;

public class when_identifying_read_model_key_from_parent_hierarchy_with_fallback_to_event_source_id : Specification
{
    AppendedEvent _rootEvent;
    AppendedEvent _childEvent;
    Key _result;
    IProjection _rootProjection;
    IProjection _childProjection;
    IEventSequenceStorage _storage;
    ISink _sink;
    KeyResolvers _keyResolvers;

    static EventType _rootEventType = new("5f4f4368-6989-4d9d-a84e-7393e0b41cfd", 1);
    static EventType _childEventType = new("02405794-91e7-4e4f-8ad1-f043070ca297", 1);
    const string ParentKey = "61fcc353-3478-4cf9-a783-da508013b36f";
    const string ChildKey = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

        // Root event with EventSourceId = ParentKey
        _rootEvent = new(
            new(
                _rootEventType,
                EventSourceType.Default,
                ParentKey, // EventSourceId is the ParentKey
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System),
            new ExpandoObject());

        // Child event appended to the SAME EventSourceId as parent (ParentKey)
        // but event content does NOT contain a property matching parent's identifier ("id")
        _childEvent = new(
            new(
                _childEventType,
                EventSourceType.Default,
                ParentKey, // Same EventSourceId as root event
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
                // Note: no "id" property - only childId and name
                childId = ChildKey,
                name = "Test Child"
            }.AsExpandoObject());

        _rootProjection = Substitute.For<IProjection>();
        _rootProjection.EventTypes.Returns([_rootEventType]);
        _rootProjection.OwnEventTypes.Returns([_rootEventType]);
        _rootProjection.IdentifiedByProperty.Returns((PropertyPath)"id");
        _rootProjection.Path.Returns((ProjectionPath)"root");

        _childProjection = Substitute.For<IProjection>();
        _childProjection.HasParent.Returns(true);
        _childProjection.Parent.Returns(_rootProjection);
        _childProjection.ChildrenPropertyPath.Returns((PropertyPath)"children");
        _childProjection.Path.Returns((ProjectionPath)"root -> children");

        _storage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();

        // When looking up parent event by EventSourceId (ParentKey), return the root event
        _storage.TryGetLastInstanceOfAny(ParentKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.Contains(_rootEventType.Id))).Returns(_rootEvent);

        // Root projection's key resolver returns the EventSourceId as the key
        _rootProjection.GetKeyResolverFor(_rootEventType).Returns(_ => (_, __, @event) => Task.FromResult(KeyResolverResult.Resolved(new Key(@event.Context.EventSourceId.ToString(), ArrayIndexers.NoIndexers))));
    }

    async Task Because()
    {
        // Create a value provider that tries to get "id" from event content
        // Since "id" doesn't exist in _childEvent.Content, it should return null
        var valueProvider = EventValueProviders.EventContent("id");

        // Use the fallback key resolver which falls back to EventSourceId if value provider returns null
        var parentKeyResolver = _keyResolvers.FromEventValueProviderWithFallbackToEventSourceId(valueProvider);

        var keyResult = await _keyResolvers.FromParentHierarchy(
            _childProjection,
            _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("childId")),
            parentKeyResolver,
            "childId")(_storage, _sink, _childEvent);
        _result = (keyResult as ResolvedKey)!.Key;
    }

    [Fact] void should_return_parent_key_as_the_root_key() => _result.Value.ShouldEqual(ParentKey);
    [Fact] void should_have_one_array_indexer() => _result.ArrayIndexers.All.Count().ShouldEqual(1);
    [Fact] void should_have_array_indexer_with_child_key() => _result.ArrayIndexers.All.First().Identifier.ShouldEqual(ChildKey);
}
