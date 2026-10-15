// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_scope_filters_event_types : given.a_before_first_batch
{
    void Establish()
    {
        _history.Add(EventToAppendFor(_eventSourceId) with { EventType = new EventType("another-event", EventTypeGeneration.First) });
        _scopes[_eventSourceId] = _scopes[_eventSourceId] with { EventTypes = [_eventType] };
    }

    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_accept_a_source_with_only_other_event_types() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_all_events() => _storedBatch.Length.ShouldEqual(3);
    [Fact] void should_check_the_filtered_source_only_once() => _checkedSources.ShouldContainOnly(_eventSourceId);
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
