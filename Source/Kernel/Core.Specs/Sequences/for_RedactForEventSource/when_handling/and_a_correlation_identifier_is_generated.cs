// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_RedactForEventSource.when_handling;

public class and_a_correlation_identifier_is_generated : Sequences.given.an_append_endpoint
{
    readonly CorrelationId _correlationId = new(Guid.Parse("25cbd6b6-416a-4c2e-9c13-a8e688c9828b"));

    async Task Because() => await new RedactForEventSource("store", "namespace", "event-log", "source", "Remove personal data", [])
        .Handle(_grainFactory, _causation, _principal, _correlationId);

    [Fact] void should_use_the_generated_correlation_identifier() => _eventSequence.ReceivedCalls()
        .Single(call => call.GetMethodInfo().Name == nameof(IEventSequence.Append)).GetArguments()[2].ShouldEqual(_correlationId);
}
