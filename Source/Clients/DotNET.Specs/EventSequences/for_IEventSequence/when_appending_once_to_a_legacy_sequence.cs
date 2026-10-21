// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

public class when_appending_once_to_a_legacy_sequence : given.a_legacy_event_sequence
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _sequence.AppendOnce("source", "event", new IdempotencyKey("k")));

    [Fact] void should_throw_a_dedicated_exception() => _error.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
    [Fact] void should_not_call_the_legacy_implementation() => _implementation.AppendCalls.ShouldEqual(0);
    [Fact] void should_not_advertise_support() => _sequence.SupportsIdempotentAppends.ShouldBeFalse();
}
