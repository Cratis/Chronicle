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
    ISink _sink;
    Key _result;

    static EventType _rootEventType = new("root", 1);
    static EventType _firstLevelEventType = new("first-level", 1);
    static EventType _secondLevelEventType = new("second-level", 1);
    static EventType _thirdLevelEventType = new("third-level", 1);
    static EventType _forthLevelEventType = new("forth-level", 1);

    const string RootKey = "root";
    const string FirstLevelKey = "first-level";
    const string SecondLevelKey = "second-level";
    const string ThirdLevelKey = "third-level";
    const string ForthLevelKey = "forth-level";

    AppendedEvent CreateEvent(EventSequenceNumber sequenceNumber, EventType type, EventSourceId eventSourceId, object content)
    {
        return new(
            new(
                type,
                EventSourceType.Default,
                eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                sequenceNumber,
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
            projection.GetKeyResolverFor(eventType).Returns((_, __, ___) => Task.FromResult(KeyResolverResult.Resolved(new Key(key, ArrayIndexers.NoIndexers))));
        }
        return projection;
    }

    void Establish()
    {
        _keyResolvers = new KeyResolvers(NullLogger<KeyResolvers>.Instance);
        _rootEvent = CreateEvent(0, _rootEventType, RootKey, new { });
        _firstLevelEvent = CreateEvent(1, _firstLevelEventType, FirstLevelKey, new { parentId = RootKey });
        _secondLevelEvent = CreateEvent(2, _secondLevelEventType, SecondLevelKey, new { parentId = FirstLevelKey });
        _thirdLevelEvent = CreateEvent(3, _thirdLevelEventType, ThirdLevelKey, new { parentId = SecondLevelKey });
        _forthLevelEvent = CreateEvent(4, _forthLevelEventType, ForthLevelKey, new { parentId = ThirdLevelKey });

        _rootProjection = SetupProjection(_rootEventType, RootKey);
        _firstLevelProjection = SetupProjection(_firstLevelEventType, FirstLevelKey, "firstLevels", _rootProjection);
        _secondLevelProjection = SetupProjection(_secondLevelEventType, SecondLevelKey, "secondLevels", _firstLevelProjection);
        _thirdLevelProjection = SetupProjection(_thirdLevelEventType, ThirdLevelKey, "thirdLevels", _secondLevelProjection);
        _forthLevelProjection = SetupProjection(_forthLevelEventType, ForthLevelKey, "forthLevels", _thirdLevelProjection);

        _storage = Substitute.For<IEventSequenceStorage>();
        _sink = Substitute.For<ISink>();
        _storage.TryGetLastInstanceOfAny(RootKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _rootEventType.Id }))).Returns(new Option<AppendedEvent>(_rootEvent));
        _storage.TryGetLastInstanceOfAny(FirstLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _firstLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_firstLevelEvent));
        _storage.TryGetLastInstanceOfAny(SecondLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _secondLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_secondLevelEvent));
        _storage.TryGetLastInstanceOfAny(ThirdLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _thirdLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_thirdLevelEvent));
        _storage.TryGetLastInstanceOfAny(ForthLevelKey, Arg.Is<IEnumerable<EventTypeId>>(x => x.SequenceEqual(new List<EventTypeId>() { _forthLevelEventType.Id }))).Returns(new Option<AppendedEvent>(_forthLevelEvent));
    }

    async Task Because()
    {
        var keyResult = await _keyResolvers.FromParentHierarchy(
            _forthLevelProjection,
            _keyResolvers.FromEventSourceId,
            _keyResolvers.FromEventValueProvider(EventValueProviders.EventContent("parentId")),
            "childId")(_storage, _sink, _forthLevelEvent);
        _result = (keyResult as ResolvedKey)!.Key;
    }

    [Fact] void should_return_expected_key() => _result.Value.ShouldEqual(RootKey);
    [Fact] void should_hold_array_indexer_for_first_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "firstLevels").Identifier.ToString().ShouldEqual(FirstLevelKey);
    [Fact] void should_hold_array_indexer_for_second_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "secondLevels").Identifier.ToString().ShouldEqual(SecondLevelKey);
    [Fact] void should_hold_array_indexer_for_third_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "thirdLevels").Identifier.ToString().ShouldEqual(ThirdLevelKey);
    [Fact] void should_hold_array_indexer_for_forth_level_with_correct_identifier() => _result.ArrayIndexers.All.Single(_ => _.ArrayProperty == "forthLevels").Identifier.ToString().ShouldEqual(ForthLevelKey);
}
