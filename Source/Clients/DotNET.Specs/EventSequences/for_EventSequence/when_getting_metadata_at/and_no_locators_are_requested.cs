// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_no_locators_are_requested : given.an_event_sequence
{
    IImmutableDictionary<EventSequenceNumber, EventMetadata> _result;

    async Task Because() => _result = await _eventSequence.GetMetadataAt([]);

    [Fact] void should_return_an_empty_result() => _result.ShouldBeEmpty();
    [Fact] void should_not_call_the_kernel() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
