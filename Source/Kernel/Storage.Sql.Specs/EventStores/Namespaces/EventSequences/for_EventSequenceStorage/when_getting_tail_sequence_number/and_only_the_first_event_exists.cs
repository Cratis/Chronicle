// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_tail_sequence_number;

public class and_only_the_first_event_exists : given.a_storage_with_tail_events
{
    EventSequenceNumber _result;

    void Establish() => SeedEvent(0);

    async Task Because() => _result = await _storage.GetTailSequenceNumber();

    [Fact] void should_be_sequence_number_zero_and_not_unavailable() => _result.ShouldEqual((EventSequenceNumber)0UL);
}
