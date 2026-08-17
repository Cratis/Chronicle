// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

/// <summary>
/// When the configured parent key expression resolves to no value — the event does not carry the
/// property named as <c>parent</c> in the projection definition — the resolution must fail with a
/// descriptive exception naming the projection and the event, not a bare
/// <see cref="ArgumentNullException"/> from deep inside the event-sequence lookup (issue #3725).
/// </summary>
public class when_identifying_read_model_key_for_child_event_without_parent_key : Specification
{
    const string ChildKey = "child-key-789";
    const string BoardEventSource = "board-event-source";
    static readonly EventType _childEventType = new("02405794-91e7-4e4f-8ad1-f043070ca297", 1);
    static readonly EventType _rootEventType = new("5f4f4368-6989-4d9d-a84e-7393e0b41cfd", 1);

    AppendedEvent _event;
    IProjection _rootProjection;
    IProjection _childProjection;
    IEventSequenceStorage _storage;
    ISink _sink;
    KeyResolvers _keyResolvers;
    Exception _error;

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);

        _event = new(
            new(
                _childEventType,
                EventSourceType.Default,
                BoardEventSource,
                EventStreamType.All,
                EventStreamId.Default,
                3,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new
            {
                childId = ChildKey
            }.AsExpandoObject());

        _rootProjection = Substitute.For<IProjection>();
        _rootProjection.HasParent.Returns(false);
        _rootProjection.Parent.Returns((IProjection)null);
        _rootProjection.ChildrenPropertyPath.Returns(PropertyPath.NotSet);
        _rootProjection.IdentifiedByProperty.Returns((PropertyPath)"id");
        _rootProjection.Path.Returns((ProjectionPath)"root");
        _rootProjection.OwnEventTypes.Returns([_rootEventType]);
        _rootProjection.EventTypes.Returns([_rootEventType]);

        _childProjection = Substitute.For<IProjection>();
        _childProjection.Identifier.Returns((ProjectionId)"my-projection");
        _childProjection.HasParent.Returns(true);
        _childProjection.Parent.Returns(_rootProjection);
        _childProjection.ChildrenPropertyPath.Returns((PropertyPath)"children");
        _childProjection.IdentifiedByProperty.Returns((PropertyPath)"childId");
        _childProjection.Path.Returns((ProjectionPath)"root -> children");
        _childProjection.OwnEventTypes.Returns([_childEventType]);
        _childProjection.EventTypes.Returns([_childEventType]);

        _storage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();
    }

    async Task Because() => _error = await Catch.Exception(async () => await _keyResolvers.FromParentHierarchy(
        _childProjection,
        _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("childId")),
        _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
        "childId")(_storage, _sink, _event));

    [Fact] void should_throw_missing_parent_key_for_child_event() => _error.ShouldBeOfExactType<MissingParentKeyForChildEvent>();
    [Fact] void should_carry_the_projection_identifier() => ((MissingParentKeyForChildEvent)_error).ProjectionIdentifier.ShouldEqual((ProjectionId)"my-projection");
    [Fact] void should_carry_the_projection_path() => ((MissingParentKeyForChildEvent)_error).ProjectionPath.ShouldEqual((ProjectionPath)"root -> children");
    [Fact] void should_carry_the_children_property_path() => ((MissingParentKeyForChildEvent)_error).ChildrenPropertyPath.ShouldEqual((PropertyPath)"children");
    [Fact] void should_carry_the_event_type() => ((MissingParentKeyForChildEvent)_error).EventTypeId.ShouldEqual(_childEventType.Id);
    [Fact] void should_carry_the_sequence_number() => ((MissingParentKeyForChildEvent)_error).SequenceNumber.ShouldEqual(new EventSequenceNumber(3));
}
