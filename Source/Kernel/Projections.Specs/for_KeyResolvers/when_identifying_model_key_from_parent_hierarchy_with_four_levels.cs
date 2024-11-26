// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Projections.for_KeyResolvers;

public class when_identifying_model_key_from_parent_hierarchy_with_four_levels : Specification
{
    KeyResolvers _keyResolvers;
    AppendedEvent _rootEvent;
    AppendedEvent _firstLevelEvent;
    AppendedEvent _secondLevelEvent;
    AppendedEvent _thirdLevelEvent;
    AppendedEvent _forthLevelEvent;

    IProjection _rootProjection;
    IProjection _firstLevelProjection;
    IProjection _secondLevelProjection;
    IProjection _thirdLevelProjection;
    IProjection _forthLevelProjection;

    IEventSequenceStorage _storage;
    Key _result;

    static EventType _rootEventType = new("root", 1);
    static EventType _firstLevelEventType = new("first-level", 1);
    static EventType _secondLevelEventType = new("second-level", 1);
    static EventType _thirdLevelEventType = new("third-level", 1);
    static EventType _forthLevelEventType = new("forth-level", 1);

    const string _rootKey = "root";
    const string _firstLevelKey = "first-level";
    const string _secondLevelKey = "second-level";
    const string _thirdLevelKey = "third-level";
    const string _forthLevelKey = "forth-level";

    AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber, EventType type, EventSourceId eventSourceId, object content)
    {
        return new(
            new(sequenceNumber, type),
            new(
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                0,
                DateTimeOffset.UtcNow,
                "123b8935-a1a4-410d-aace-e340d48f0aa0",
                "41f18595-4748-4b01-88f7-4c0d0907aa90",
                CorrelationId.New(),
                [],
                Identity.System),
            content.AsExpandoObject());
    }

    IProjection SetupProjection(EventType eventType, string key, string childrenProperty = "no-levels", IProjection? parent = null)
    {
        var projection = Substitute.For<IProjection>();
        projection.EventTypes.Returns([eventType]);
        projection.OwnEventTypes.Returns([eventType]);
        projection.Path.Returns((ProjectionPath)childrenProperty);
        projection.ChildrenPropertyPath.Returns((PropertyPath)childrenProperty);

        if (parent is not null)
        {
            projection.GetKeyResolverFor(eventType).Returns(_keyResolvers.FromParentHierarchy(
                projection,
                _keyResolvers.FromEventSourceId,
                _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
                "childId"));
            projection.HasParent.Returns(true);
            projection.Parent.Returns(parent);
        }
        else
        {
            projection.GetKeyResolverFor(eventType).Returns((_, __) => Task.FromResult(new Key(key, ArrayIndexers.NoIndexers)));
        }
        return projection;
    }

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        _rootEvent = CreateEvent(0, _rootEventType, _rootKey, new { });
        _firstLevelEvent = CreateEvent(1, _firstLevelEventType, _firstLevelKey, new { parentId = _rootKey });
        _secondLevelEvent = CreateEvent(2, _secondLevelEventType, _secondLevelKey, new { parentId = _firstLevelKey });
        _thirdLevelEvent = CreateEvent(3, _thirdLevelEventType, _thirdLevelKey, new { parentId = _secondLevelKey });
        _forthLevelEvent = CreateEvent(4, _forthLevelEventType, _forthLevelKey, new { parentId = _thirdLevelKey });

        _rootProjection = SetupProjection(_rootEventType, _rootKey);
        _firstLevelProjection = SetupProjection(_firstLevelEventType, _firstLevelKey, "firstLevels", _rootProjection);
        _secondLevelProjection = SetupProjection(_secondLevelEventType, _secondLevelKey, "secondLevels", _firstLevelProjection);
        _thirdLevelProjection = SetupProjection(_thirdLevelEventType, _thirdLevelKey, "thirdLevels", _secondLevelProjection);
        _forthLevelProjection = SetupProjection(_forthLevelEventType, _forthLevelKey, "forthLevels", _thirdLevelProjection);

        _storage = Substitute.For<IEventSequenceStorage>();
        _storage.TryGetLastInstanceOfAny(_rootKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _rootEventType.Id }))).Returns(new Option<AppendedEvent>(_rootEvent));
        _storage.TryGetLastInstanceOfAny(_firstLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _firstLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_firstLevelEvent));
        _storage.TryGetLastInstanceOfAny(_secondLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _secondLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_secondLevelEvent));
        _storage.TryGetLastInstanceOfAny(_thirdLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _thirdLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_thirdLevelEvent));
        _storage.TryGetLastInstanceOfAny(_forthLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() {_forthLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_forthLevelEvent));
    }

    async Task Because() => _result = await _keyResolvers.FromParentHierarchy(
        _forthLevelProjection,
        _keyResolvers.FromEventSourceId,
        _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
        "childId")(_storage, _forthLevelEvent);

    [Fact] void should_return_expected_key() => _result.Value.ShouldEqual(_rootKey);
    [Fact] void should_hold_array_indexer_for_first_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "firstLevels").Identifier.ToString().ShouldEqual(_firstLevelKey);
    [Fact] void should_hold_array_indexer_for_second_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "secondLevels").Identifier.ToString().ShouldEqual(_secondLevelKey);
    [Fact] void should_hold_array_indexer_for_third_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "thirdLevels").Identifier.ToString().ShouldEqual(_thirdLevelKey);
    [Fact] void should_hold_array_indexer_for_forth_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "forthLevels").Identifier.ToString().ShouldEqual(_forthLevelKey);
}
