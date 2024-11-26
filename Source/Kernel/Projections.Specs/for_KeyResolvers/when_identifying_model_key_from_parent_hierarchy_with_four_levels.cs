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

    static EventType _rootEventType = new("5f4f4368-6989-4d9d-a84e-7393e0b41cfd", 1);
    static EventType _firstLevelEventType = new("eef0f7c0-25eb-48dc-b824-7e27ba4593f2", 1);
    static EventType _secondLevelEventType = new("6682281b-64e0-431b-8b90-dd49ba25ca55", 1);
    static EventType _thirdLevelEventType = new("90ae84f7-84f0-4e3a-a38a-329d782da158", 1);
    static EventType _forthLevelEventType = new("e72686b1-b0e4-40a8-9651-4841602638da", 1);

    const string _rootKey = "4e3a1e36-714c-41f7-83e3-5cc84717db16";
    const string _firstLevelKey = "805908d8-ed72-4a70-b313-6f592632663d";
    const string _secondLevelKey = "02311df8-fcf2-42ff-b2d0-b7fa2f576485";
    const string _thirdLevelKey = "935e1158-cedb-4530-a9aa-d925d6d9b10d";
    const string _forthLevelKey = "537d661f-8b12-4a1e-917b-faf639923380";

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
        _storage.TryGetLastInstanceOfAny(_rootKey, [_rootEventType.Id]).Returns(Task.FromResult<Option<AppendedEvent>>(_rootEvent));
        _storage.TryGetLastInstanceOfAny(_firstLevelKey, [_firstLevelEventType.Id]).Returns(Task.FromResult<Option<AppendedEvent>>(_firstLevelEvent));
        _storage.TryGetLastInstanceOfAny(_secondLevelKey, [_secondLevelEventType.Id]).Returns(Task.FromResult<Option<AppendedEvent>>(_secondLevelEvent));
        _storage.TryGetLastInstanceOfAny(_thirdLevelKey, [_thirdLevelEventType.Id]).Returns(Task.FromResult<Option<AppendedEvent>>(_thirdLevelEvent));
        _storage.TryGetLastInstanceOfAny(_forthLevelKey, [_forthLevelEventType.Id]).Returns(Task.FromResult<Option<AppendedEvent>>(_forthLevelEvent));
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
