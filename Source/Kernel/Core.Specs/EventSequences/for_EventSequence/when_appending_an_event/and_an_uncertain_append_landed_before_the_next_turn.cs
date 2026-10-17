// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_an_uncertain_append_landed_before_the_next_turn : given.an_append_with_settlement
{
    async Task Establish()
    {
        _throwAfterWrite = true;
        _probeFails = true;
        var first = await AppendAnEvent();
        first.Errors.ShouldContain(AppendError.AppendOutcomeUnknown);
        _failReads = true;
    }
    async Task Because() => _result = await AppendAnEvent();
    [Fact] void should_refresh_the_tail_before_refusing_the_next_append() => _stateStorage.State.SequenceNumber.ShouldEqual((EventSequenceNumber)1UL);
    [Fact] void should_refuse_until_the_index_is_repaired() => _result.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_not_append_again() => _log.Events.Count.ShouldEqual(1);
}
