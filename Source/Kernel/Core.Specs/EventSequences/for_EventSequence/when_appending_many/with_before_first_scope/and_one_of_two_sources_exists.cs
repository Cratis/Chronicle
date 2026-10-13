// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_one_of_two_sources_exists : given.a_before_first_batch
{
    readonly EventSourceId _otherSource = "another-source";

    void Establish()
    {
        _events.Add(EventToAppendFor(_otherSource));
        _scopes[_otherSource] = new(EventSequenceNumber.BeforeFirst, true, null, null, null, null);
        _history.Add(EventToAppendFor(_otherSource));
    }

    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_reject_the_whole_batch() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_only_the_existing_source() => _result.ConcurrencyViolations.ShouldContainOnly(new ConcurrencyViolation(_otherSource, EventSequenceNumber.BeforeFirst, 5));
    [Fact] void should_not_append_even_the_new_source() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_check_both_sources() => _checkedSources.ShouldContainOnly(_eventSourceId, _otherSource);
    [Fact] void should_return_no_sequence_numbers() => _result.SequenceNumbers.ShouldBeEmpty();
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
