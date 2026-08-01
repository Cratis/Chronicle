// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_getting_the_tail_sequence_number;

/// <summary>
/// An event source type narrows the tail only when it is an actual type. The default and unspecified values
/// mean "do not narrow", which is what a caller asking for the tail of everything passes — so treating them as
/// a value to match would answer with the tail of whatever happens to carry the sentinel, and treating an
/// actual type as no filter would answer with the tail across every type. This storage is the one the server
/// runs on; the SQL and in-memory implementations already read the sentinel this way.
/// </summary>
/// <param name="fixture">The <see cref="ReplicaSetMongoDBFixture"/> supplying the database.</param>
[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_events_have_different_event_source_types(ReplicaSetMongoDBFixture fixture) : given.a_replica_set_event_sequence_storage(fixture)
{
    static readonly EventSourceType _firstEventSourceType = new("Alpha");
    static readonly EventSourceType _secondEventSourceType = new("Beta");

    EventSequenceNumber _tailForFirstType;
    EventSequenceNumber _tailForSecondType;
    EventSequenceNumber _tailWithoutNarrowing;

    async Task Establish() =>
        await _storage.AppendMany(
        [
            EventAt(EventSequenceNumber.First, _eventType, _firstEventSourceType),
            EventAt(EventSequenceNumber.First + 1, _eventType, _secondEventSourceType),
            EventAt(EventSequenceNumber.First + 2, _eventType, _secondEventSourceType)
        ]);

    async Task Because()
    {
        _tailForFirstType = await _storage.GetTailSequenceNumber(eventSourceType: _firstEventSourceType);
        _tailForSecondType = await _storage.GetTailSequenceNumber(eventSourceType: _secondEventSourceType);
        _tailWithoutNarrowing = await _storage.GetTailSequenceNumber(eventSourceType: EventSourceType.Default);
    }

    [Fact] void should_narrow_to_the_first_event_source_type() => _tailForFirstType.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_narrow_to_the_second_event_source_type() => _tailForSecondType.ShouldEqual((EventSequenceNumber)2);
    [Fact] void should_not_narrow_on_the_sentinel() => _tailWithoutNarrowing.ShouldEqual((EventSequenceNumber)2);
}
