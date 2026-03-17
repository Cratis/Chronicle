// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Driver;
using KernelObserverDefinition = Cratis.Chronicle.Storage.Observation.ObserverDefinition;

namespace Cratis.Chronicle.Storage.MongoDB.Observation.for_ObserverDefinitionsStorage.when_getting_replayable_observers_for_event_types;

public class and_no_observers_match : given.an_observer_definitions_storage
{
    static readonly EventTypeId _targetEventTypeId = new("f6ca8ea1-dd94-4a9d-b923-e5e6d7a36b41");

    IEnumerable<KernelObserverDefinition> _result;
    IAsyncCursor<ObserverDefinition> _cursor;

    void Establish()
    {
        _cursor = Substitute.For<IAsyncCursor<ObserverDefinition>>();
        _cursor.Current.Returns([]);
        _cursor.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(true), Task.FromResult(false));

        _collection.FindAsync(
            Arg.Any<FilterDefinition<ObserverDefinition>>(),
            Arg.Any<FindOptions<ObserverDefinition, ObserverDefinition>>(),
            Arg.Any<CancellationToken>()).Returns(Task.FromResult(_cursor));
    }

    async Task Because() => _result = await _storage.GetReplayableObserversForEventTypes(
        [new EventType(_targetEventTypeId, EventTypeGeneration.First)]);

    [Fact]
    void should_return_empty_collection() => _result.ShouldBeEmpty();
}
