// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

/// <summary>
/// Same stale-tail livelock as the single append, exercised through the batch path: the stored tail
/// is 2 but the batch starts at an already-occupied number. The returned
/// <see cref="DuplicateEventSequenceNumber"/> must carry the true next-available slot (3), not the
/// occupied number the batch attempted. Before the fix the SQL backend returned the occupied number.
/// </summary>
public class and_a_sequence_number_is_a_stale_duplicate : given.an_event_sequence_storage
{
    static readonly EventSequenceNumber _tailSequenceNumber = new(2);
    static readonly EventSequenceNumber _staleSequenceNumber = new(1);
    Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber> _result;

    void Establish()
    {
        SeedEvent(EventSequenceNumber.First);
        SeedEvent(_staleSequenceNumber);
        SeedEvent(_tailSequenceNumber);
    }

    async Task Because() => _result = await AppendMany(_staleSequenceNumber);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_next_available_after_the_tail() => _result.AsT1.NextAvailableSequenceNumber.ShouldEqual(_tailSequenceNumber.Next());
    [Fact] void should_not_report_the_occupied_sequence_number() => _result.AsT1.NextAvailableSequenceNumber.Value.ShouldBeGreaterThan(_staleSequenceNumber.Value);
}
