// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_source_exists : given.a_before_first_batch
{
    void Establish() => _history.Add(EventToAppendFor(_eventSourceId));

    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_reject_the_batch() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_return_the_concurrency_violation() => _result.ConcurrencyViolations.ShouldContainOnly(new ConcurrencyViolation(_eventSourceId, EventSequenceNumber.BeforeFirst, 5));
    [Fact] void should_not_append_any_event() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_return_no_sequence_numbers() => _result.SequenceNumbers.ShouldBeEmpty();
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
