// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Driver;
using KernelObserverDefinition = Cratis.Chronicle.Storage.Observation.ObserverDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverDefinitionsStorage.when_getting_replayable_observers_for_event_types;

public class and_there_are_matching_replayable_observers : given.an_observer_definitions_storage
{
    static readonly EventTypeId _targetEventTypeId = new("f6ca8ea1-dd94-4a9d-b923-e5e6d7a36b41");
    static readonly EventTypeId _otherEventTypeId = new("c7b1e7a2-1234-4a9d-b923-aabbccddeeff");

    IEnumerable<KernelObserverDefinition> _result;
    IAsyncCursor<ObserverDefinition> _cursor;

    ObserverDefinition _replayableMatchingObserver;
    ObserverDefinition _replayableNonMatchingObserver;
    ObserverDefinition _nonReplayableMatchingObserver;

    void Establish()
    {
        _replayableMatchingObserver = new ObserverDefinition
        {
            Id = "replayable-matching",
            IsReplayable = true,
            EventTypes = new Dictionary<string, string> { { _targetEventTypeId.Value, "$eventSourceId" } },
            EventSequenceId = EventSequenceId.Log,
            Type = ObserverType.Reactor,
            Owner = ObserverOwner.None
        };
        _replayableNonMatchingObserver = new ObserverDefinition
        {
            Id = "replayable-non-matching",
            IsReplayable = true,
            EventTypes = new Dictionary<string, string> { { _otherEventTypeId.Value, "$eventSourceId" } },
            EventSequenceId = EventSequenceId.Log,
            Type = ObserverType.Reactor,
            Owner = ObserverOwner.None
        };
        _nonReplayableMatchingObserver = new ObserverDefinition
        {
            Id = "non-replayable-matching",
            IsReplayable = false,
            EventTypes = new Dictionary<string, string> { { _targetEventTypeId.Value, "$eventSourceId" } },
            EventSequenceId = EventSequenceId.Log,
            Type = ObserverType.Reactor,
            Owner = ObserverOwner.None
        };

        _cursor = Substitute.For<IAsyncCursor<ObserverDefinition>>();
        _cursor.Current.Returns([_replayableMatchingObserver, _replayableNonMatchingObserver]);
        _cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));

        _collection.FindAsync(
            Arg.Any<FilterDefinition<ObserverDefinition>>(),
            Arg.Any<FindOptions<ObserverDefinition, ObserverDefinition>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(_cursor));
    }

    async Task Because() => _result = await _storage.GetReplayableObserversForEventTypes(
        [new EventType(_targetEventTypeId, EventTypeGeneration.First)]);

    [Fact]
    void should_return_only_one_observer() => _result.Count().ShouldEqual(1);

    [Fact]
    void should_return_the_replayable_matching_observer() => _result.First().Identifier.Value.ShouldEqual("replayable-matching");

    [Fact]
    void should_not_include_non_matching_observer() => _result.Any(o => o.Identifier.Value == "replayable-non-matching").ShouldBeFalse();

    [Fact]
    void should_have_queried_the_collection() => _collection.Received(1).FindAsync(
        Arg.Any<FilterDefinition<ObserverDefinition>>(),
        Arg.Any<FindOptions<ObserverDefinition, ObserverDefinition>>(),
        Arg.Any<CancellationToken>());
}
