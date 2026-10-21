// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_events_span_two_new_sources : given.a_before_first_batch
{
    readonly EventSourceId _otherSource = "another-source";

    void Establish()
    {
        _events.Add(EventToAppendFor(_otherSource));
        _scopes[_otherSource] = new(EventSequenceNumber.BeforeFirst, true, null, null, null, null);
    }

    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_accept_the_batch() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_events_for_both_sources() => _storedBatch.Select(@event => @event.EventSourceId).Distinct().ShouldContainOnly(_eventSourceId, _otherSource);
    [Fact] void should_append_all_four_events() => _storedBatch.Length.ShouldEqual(4);
    [Fact] void should_check_each_source_once() => _checkedSources.ShouldContainOnly(_eventSourceId, _otherSource);
    [Fact] void should_check_both_sources_before_writing() => _checksBeforeWrite.ShouldEqual(2);
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
