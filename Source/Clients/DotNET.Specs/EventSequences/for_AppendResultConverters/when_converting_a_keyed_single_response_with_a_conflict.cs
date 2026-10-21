// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_keyed_single_response_with_a_conflict : Specification
{
    AppendResult _result;

    void Because() => _result = new Contracts.Sequences.AppendOnceResponse
    {
        Outcome = Contracts.EventSequences.AppendOutcome.Committed,
        Errors = ["The key is in use"],
        IdempotencyConflicts =
        [
            new()
            {
                Key = "order-1",
                ExistingSequenceNumber = 3,
                StoredEventTypeId = "created",
                StoredEventTypeGeneration = 1,
                RequestedGeneration = 2,
                Reason = Contracts.EventSequences.IdempotencyConflictReason.Generation
            }
        ]
    }.ToClient();

    [Fact] void should_report_the_conflict() => _result.HasIdempotencyConflicts.ShouldBeTrue();
    [Fact] void should_map_the_key() => _result.IdempotencyConflicts.Single().Key.Value.ShouldEqual("order-1");
    [Fact] void should_map_the_existing_sequence_number() => _result.IdempotencyConflicts.Single().ExistingSequenceNumber.Value.ShouldEqual(3UL);
    [Fact] void should_map_the_stored_event_type() => _result.IdempotencyConflicts.Single().StoredEventType.ShouldEqual(new Events.EventType("created", 1u));
    [Fact] void should_map_the_requested_generation() => _result.IdempotencyConflicts.Single().RequestedGeneration.Value.ShouldEqual(2u);
    [Fact] void should_map_the_reason() => _result.IdempotencyConflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Generation);
    [Fact] void should_not_be_a_success() => _result.IsSuccess.ShouldBeFalse();
}
