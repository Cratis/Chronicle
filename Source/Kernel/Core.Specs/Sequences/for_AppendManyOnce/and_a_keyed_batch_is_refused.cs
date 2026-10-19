// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnce;

public class and_a_keyed_batch_is_refused : Sequences.given.an_append_endpoint
{
    AppendManyOnceResult _result;

    void Establish() =>
        _eventSequence.AppendManyWithIdempotencyKeys(
            Arg.Any<IEnumerable<EventSequences.EventToAppend>>(),
            Arg.Any<IReadOnlyList<IdempotencyKey?>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<ConcurrencyScopes>()).Returns(Task.FromResult(new IdempotentAppendManyResult(
                new AppendManyResult { Errors = [new AppendError("IdempotencyKeyRepeatedInBatch: repeated")] })));

    async Task Because() => _result = await new AppendManyOnce(
        "store",
        "namespace",
        "event-log",
        [new(new EventForEventSourceId("source", "", "", "", new EventType("event", 1, false), "{}"), "k")]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_the_refusal_code() => _result.Errors.Single().Value.StartsWith("IdempotencyKeyRepeatedInBatch").ShouldBeTrue();
}
