// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_IEventSequence;

public class when_completing_a_scope_on_a_legacy_sequence : given.a_legacy_event_sequence
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _sequence.CompleteStream(ClosedStreamScope.ForEventSource(EventSourceId.New())));

    [Fact] void should_throw_a_dedicated_exception() => _error.ShouldBeOfExactType<ClosedStreamScopesNotSupported>();
}
