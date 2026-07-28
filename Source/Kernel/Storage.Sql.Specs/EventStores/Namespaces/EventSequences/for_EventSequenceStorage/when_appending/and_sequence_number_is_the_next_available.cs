// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending;

/// <summary>
/// A fresh append at the correct next-available sequence number (one past the stored tail) succeeds.
/// </summary>
public class and_sequence_number_is_the_next_available : given.an_event_sequence_storage
{
    static readonly EventSequenceNumber _tailSequenceNumber = new(2);
    Result<AppendedEvent, DuplicateEventSequenceNumber> _result;

    void Establish()
    {
        SeedEvent(EventSequenceNumber.First);
        SeedEvent(new EventSequenceNumber(1));
        SeedEvent(_tailSequenceNumber);
    }

    async Task Because() => _result = await Append(_tailSequenceNumber.Next());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_at_the_next_available_sequence_number() => _result.AsT0.Context.SequenceNumber.ShouldEqual(_tailSequenceNumber.Next());
}
