// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_metadata_at;

public class and_locators_exceed_the_limit : given.an_event_sequence
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _eventSequence.GetMetadataAt(Enumerable.Range(0, 501).Select(_ => (EventSequenceNumber)(ulong)_)));

    [Fact] void should_refuse_before_calling_the_kernel() => _error.ShouldBeOfExactType<TooManyEventLocators>();
    [Fact] void should_not_call_the_kernel() => _sequences.ReceivedCalls().ShouldBeEmpty();
}
