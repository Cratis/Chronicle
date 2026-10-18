// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_read_back_confirms_the_unknown_commit : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(async call =>
    {
        await _log.AppendManyWithIdentities(call.Arg<IEnumerable<EventToAppendToStorage>>());
        throw new Exception("unknown commit");
    });
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_report_our_append() => _result.Outcome.ShouldEqual(AppendOutcome.Appended);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_enqueue_the_confirmed_append() => await _appendedEventsQueues.Received(1).Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_need_a_second_key_lookup() => _lookups.ShouldEqual(1);
}
