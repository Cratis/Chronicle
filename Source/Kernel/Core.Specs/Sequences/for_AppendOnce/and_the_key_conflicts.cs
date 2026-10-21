// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendOnce;

public class and_the_key_conflicts : given_a_keyed_append
{
    void Establish() => ReturnSettlement(new IdempotentAppendResult(
        AppendResult.Failed(Guid.NewGuid(), [new AppendError("IdempotencyKeyConflict: refused")]))
    {
        Conflicts = [new IdempotencyConflict(new Concepts.EventSequences.IdempotencyKey("the-key"), new EventSequenceNumber(7UL), new Concepts.Events.EventType("stored", 1, false), new EventTypeGeneration(2), IdempotencyConflictReason.Generation)]
    });

    async Task Because() => await Handle();

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_the_refusal() => _result.Errors.Single().Value.StartsWith("IdempotencyKeyConflict").ShouldBeTrue();
    [Fact] void should_describe_the_key() => _result.IdempotencyConflicts.Single().Key.ShouldEqual("the-key");
    [Fact] void should_describe_the_existing_sequence_number() => _result.IdempotencyConflicts.Single().ExistingSequenceNumber.ShouldEqual(7UL);
    [Fact] void should_describe_the_stored_type() => _result.IdempotencyConflicts.Single().StoredEventTypeId.ShouldEqual("stored");
    [Fact] void should_describe_the_requested_generation() => _result.IdempotencyConflicts.Single().RequestedGeneration.ShouldEqual(2u);
    [Fact] void should_describe_the_reason() => _result.IdempotencyConflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Generation);
}
