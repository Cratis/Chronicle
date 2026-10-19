// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_keyed_batch_response : Specification
{
    AppendManyResult _result;

    void Because() => _result = new Contracts.Sequences.AppendManyOnceResponse
    {
        SequenceNumbers = [1, 2, 3],
        Outcomes =
        [
            Contracts.EventSequences.AppendOutcome.Committed,
            Contracts.EventSequences.AppendOutcome.AlreadyAppended,
            Contracts.EventSequences.AppendOutcome.AlreadyAppendedAfterErasure
        ],
        IdempotencyConflicts =
        [
            new() { Key = "k", StoredEventTypeId = "created", Reason = Contracts.EventSequences.IdempotencyConflictReason.Unverifiable }
        ]
    }.ToClient();

    [Fact] void should_align_the_outcomes_with_the_sequence_numbers() => _result.Outcomes.ToArray().ShouldEqual([AppendOutcome.Appended, AppendOutcome.AlreadyAppended, AppendOutcome.AlreadyAppendedAfterErasure]);
    [Fact] void should_not_report_all_as_already_appended() => _result.AllAlreadyAppended.ShouldBeFalse();
    [Fact] void should_map_the_conflict_reason() => _result.IdempotencyConflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Unverifiable);
}
