// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_idempotently;

public class a_batch : given.an_idempotent_append
{
    AppendManyResult _result;

    async Task Because() => _result = await _eventSequence.AppendManyOnce("source", ["one", "two"], _key);

    [Fact] void should_key_the_first_event_by_index() => _batchRequest.Events.First().IdempotencyKey.ShouldEqual(_key.ForIndex(0).Value);
    [Fact] void should_key_the_second_event_by_index() => _batchRequest.Events.Last().IdempotencyKey.ShouldEqual(_key.ForIndex(1).Value);
    [Fact] void should_report_an_outcome_per_event() => _result.Outcomes.ToArray().ShouldEqual([AppendOutcome.Appended, AppendOutcome.AlreadyAppended]);
    [Fact] void should_notify_only_for_the_appended_event() => _notifications.ShouldEqual(1);
}
