// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

/// <summary>
/// A fresh batch starting at the correct next-available sequence number (one past the stored tail)
/// succeeds and appends every event in the batch.
/// </summary>
public class and_sequence_numbers_are_the_next_available : given.an_event_sequence_storage
{
    static readonly EventSequenceNumber _tailSequenceNumber = new(2);
    Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber> _result;

    void Establish()
    {
        SeedEvent(EventSequenceNumber.First);
        SeedEvent(new EventSequenceNumber(1));
        SeedEvent(_tailSequenceNumber);
    }

    async Task Because() => _result = await AppendMany(_tailSequenceNumber.Next(), new EventSequenceNumber(4));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_every_event_in_the_batch() => _result.AsT0.Count().ShouldEqual(2);
}
