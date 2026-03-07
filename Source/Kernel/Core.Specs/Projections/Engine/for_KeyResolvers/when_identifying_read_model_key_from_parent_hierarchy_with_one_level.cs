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
using Cratis.Monads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers;

public class when_identifying_read_model_key_from_parent_hierarchy_with_one_level : Specification
{
    AppendedEvent _rootEvent;
    AppendedEvent _event;
    Key _result;
    IProjection _rootProjection;
    IProjection _childProjection;
    IEventSequenceStorage _storage;
    ISink _sink;
    KeyResolvers _keyResolvers;

    static EventType _rootEventType = new("5f4f4368-6989-4d9d-a84e-7393e0b41cfd", 1);
    const string ParentKey = "61fcc353-3478-4cf9-a783-da508013b36f";

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        _rootEvent = new(
            new(
                _rootEventType,
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                1,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System,
                [],
                EventHash.NotSet),
            new ExpandoObject());

        _event = new(
            new(
                new("02405794-91e7-4e4f-8ad1-f043070ca297", 1),
                EventSourceType.Default,
                "2f005aaf-2f4e-4a47-92ea-63687ef74bd4",
                EventStreamType.All,
                EventStreamId.Default,
                0,
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
                parentId = ParentKey
            }.AsExpandoObject());

        _rootProjection = Substitute.For<IProjection>();
        _rootProjection.Path.Returns(new ProjectionPath("Root"));
        _rootProjection.EventTypes.Returns(
        [
            _rootEventType
        ]);
        _rootProjection.OwnEventTypes.Returns([_rootEventType]);
        _rootProjection.IdentifiedByProperty.Returns(PropertyPath.NotSet);

        _childProjection = Substitute.For<IProjection>();
        _childProjection.Path.Returns(new ProjectionPath("Root.Children"));
        _childProjection.HasParent.Returns(true);
        _childProjection.Parent.Returns(_rootProjection);
        _childProjection.ChildrenPropertyPath.Returns((PropertyPath)"children");
        _childProjection.IdentifiedByProperty.Returns((PropertyPath)"childId");
        _storage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();

        _storage.TryGetLastInstanceOfAny(ParentKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.Contains(_rootEventType.Id))).Returns(new Option<AppendedEvent>(_rootEvent));
        _rootProjection.GetKeyResolverFor(_rootEventType).Returns(_ => (_, __, ___) => Task.FromResult(KeyResolverResult.Resolved(new Key(ParentKey, ArrayIndexers.NoIndexers))));
    }

    async Task Because()
    {
        var keyResult = await _keyResolvers.FromParentHierarchy(
            _childProjection,
            _keyResolvers.FromEventSourceId,
            _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
            "childId")(_storage, _sink, _event);
        _result = (keyResult as ResolvedKey)!.Key;
    }

    [Fact] void should_return_expected_key() => _result.Value.ShouldEqual(ParentKey);
}
