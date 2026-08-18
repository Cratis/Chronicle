// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_the_next_sequence_number_greater_or_equal_than;

/// <summary>
/// The next sequence number at or after a position is the nearest one, not the furthest. This storage is the
/// one the server runs on, and it answered with the last qualifying event where the SQL and in-memory
/// implementations answer with the first - so a caller resuming from what it was told skipped everything in
/// between, and the two implementations could not both be right.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/> supplying the database.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_several_events_qualify(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    EventSequenceNumber _next;
    EventSequenceNumber _nextFromAnExactMatch;

    async Task Establish() =>
        await _storage.AppendMany(
        [
            EventAt(EventSequenceNumber.First, _eventType),
            EventAt(EventSequenceNumber.First + 1, _eventType),
            EventAt(EventSequenceNumber.First + 2, _eventType)
        ]);

    async Task Because()
    {
        _next = await _storage.GetNextSequenceNumberGreaterOrEqualThan(EventSequenceNumber.First + 1);
        _nextFromAnExactMatch = await _storage.GetNextSequenceNumberGreaterOrEqualThan(EventSequenceNumber.First);
    }

    [Fact] void should_answer_with_the_nearest_qualifying_event() => _next.ShouldEqual((EventSequenceNumber)1);
    [Fact] void should_answer_with_the_position_itself_when_it_holds_an_event() => _nextFromAnExactMatch.ShouldEqual(EventSequenceNumber.First);
}
