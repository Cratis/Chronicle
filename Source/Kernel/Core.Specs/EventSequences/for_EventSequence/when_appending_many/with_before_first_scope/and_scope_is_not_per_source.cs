// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_scope_is_not_per_source : given.a_before_first_batch
{
    void Establish()
    {
        _history.Add(EventToAppendFor("another-source"));
        _scopes[_eventSourceId] = _scopes[_eventSourceId] with { EventSourceId = false };
    }

    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_reject_even_though_the_appended_source_is_new() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_sequence_wide_violation() => _result.ConcurrencyViolations.ShouldContainOnly(new ConcurrencyViolation(_eventSourceId, EventSequenceNumber.BeforeFirst, 5));
    [Fact] void should_not_limit_the_lookup_to_a_source() => _checkedSources.Single().ShouldBeNull();
    [Fact] void should_not_append_any_event() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
