// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendOnce;

public class and_the_key_settles_as_a_new_event : given_a_keyed_append
{
    void Establish() => ReturnSettlement(new IdempotentAppendResult(AppendResult.Success(Guid.NewGuid(), new EventSequenceNumber(5UL)), AppendOutcome.Committed));

    async Task Because() => await Handle();

    [Fact] void should_forward_the_key() => _forwardedKey.Value.ShouldEqual("the-key");
    [Fact] void should_forward_the_event_type() => _forwardedEvent.EventType.Id.Value.ShouldEqual("event");
    [Fact] void should_report_the_outcome() => _result.Outcome.ShouldEqual(AppendOutcome.Committed);
    [Fact] void should_report_the_sequence_number() => _result.SequenceNumber.Value.ShouldEqual(5UL);
    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
