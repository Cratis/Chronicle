// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_keyed_single_response : Specification
{
    AppendResult _result;

    void Because() => _result = new Contracts.Sequences.AppendOnceResponse
    {
        SequenceNumber = 7,
        Outcome = Contracts.EventSequences.AppendOutcome.AlreadyAppendedAfterErasure,
        IsSuccess = true,
        IdempotencyConflicts = []
    }.ToClient();

    [Fact] void should_map_the_outcome() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppendedAfterErasure);
    [Fact] void should_report_it_as_already_appended() => _result.WasAlreadyAppended.ShouldBeTrue();
    [Fact] void should_keep_the_sequence_number() => _result.SequenceNumber.Value.ShouldEqual(7UL);
    [Fact] void should_still_be_a_success() => _result.IsSuccess.ShouldBeTrue();
}
