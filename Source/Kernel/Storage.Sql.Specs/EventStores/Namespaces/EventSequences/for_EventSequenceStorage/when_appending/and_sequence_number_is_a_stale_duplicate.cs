// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending;

/// <summary>
/// The stored tail is at sequence number 2, but the caller (an append grain whose persisted
/// <c>State.SequenceNumber</c> lags the real tail) attempts to append at an already-occupied
/// number. The recovery contract requires the returned <see cref="DuplicateEventSequenceNumber"/>
/// to carry the true next-available slot (3) so the grain jumps forward — returning the occupied
/// number would livelock the sequence. Before the fix the SQL backend returned the occupied number.
/// </summary>
public class and_sequence_number_is_a_stale_duplicate : given.an_event_sequence_storage
{
    static readonly EventSequenceNumber _tailSequenceNumber = new(2);
    static readonly EventSequenceNumber _staleSequenceNumber = new(1);
    Result<AppendedEvent, DuplicateEventSequenceNumber> _result;

    void Establish()
    {
        SeedEvent(EventSequenceNumber.First);
        SeedEvent(_staleSequenceNumber);
        SeedEvent(_tailSequenceNumber);
    }

    async Task Because() => _result = await Append(_staleSequenceNumber);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_next_available_after_the_tail() => _result.AsT1.NextAvailableSequenceNumber.ShouldEqual(_tailSequenceNumber.Next());
    [Fact] void should_not_report_the_occupied_sequence_number() => _result.AsT1.NextAvailableSequenceNumber.Value.ShouldBeGreaterThan(_staleSequenceNumber.Value);
}
