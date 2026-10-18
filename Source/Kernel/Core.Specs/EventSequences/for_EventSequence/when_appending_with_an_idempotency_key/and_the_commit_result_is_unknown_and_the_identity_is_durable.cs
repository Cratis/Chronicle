// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_commit_result_is_unknown_and_the_identity_is_durable : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Throws(_ =>
    {
        WinnerAppears();
        return new Exception("unknown commit");
    });
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_report_a_durable_hit() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] async Task should_spill_before_settling() => await _appendedEventsQueues.Received(1).SpillToCatchup();
    [Fact] async Task should_not_enqueue() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
