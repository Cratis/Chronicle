// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnce;

public class and_several_scopes_target_one_event_source : Sequences.given.an_append_endpoint
{
    AppendManyOnceResult _result;

    async Task Because() => _result = await new AppendManyOnce(
        "store",
        "namespace",
        "event-log",
        [new(new EventForEventSourceId("source", "", "", "", new EventType("event", 1, false), "{}"), "k")],
        ConcurrencyScopes:
        [
            new EventSourceConcurrencyScope("source", new ConcurrencyScope(1UL, true)),
            new EventSourceConcurrencyScope("source", new ConcurrencyScope(2UL, true))
        ]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_reach_the_grain() => _eventSequence.DidNotReceive().AppendManyWithIdempotencyKeys(
        Arg.Any<IEnumerable<EventSequences.EventToAppend>>(),
        Arg.Any<IReadOnlyList<IdempotencyKey?>>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<Concepts.Identities.Identity>(),
        Arg.Any<ConcurrencyScopes>());
}
