// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_getting_tail_sequence_number;

public class and_filtering_on_an_event_type : given.a_storage_with_tail_events
{
    EventSequenceNumber _result;

    async Task Establish() => await AppendTailEvents();

    async Task Because() => _result = await _storage.GetTailSequenceNumber([_typeA]);

    [Fact] void should_return_the_expected_tail() => _result.ShouldEqual((EventSequenceNumber)2UL);
}
