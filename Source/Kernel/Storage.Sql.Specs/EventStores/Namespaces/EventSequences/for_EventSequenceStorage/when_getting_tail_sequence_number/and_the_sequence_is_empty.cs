// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_tail_sequence_number;

public class and_the_sequence_is_empty : given.a_storage_with_tail_events
{
    EventSequenceNumber _result;

    async Task Because() => _result = await _storage.GetTailSequenceNumber();

    [Fact] void should_be_unavailable() => _result.ShouldEqual(EventSequenceNumber.Unavailable);
}
