// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_an_existing_inbox_name_is_reserved : given.a_reserved_name_check
{
    bool _closedStreamsResult;
    bool _constraintResult;

    void Establish()
    {
        Reserve("inbox-orders+closed_streams");
        Reserve("inbox-orders+email+constraint");
        _namespaceStorage.GetEventSequences().Returns(Task.FromResult<IEnumerable<EventSequenceId>>(
            [new EventSequenceId("inbox-orders+closed_streams"), new EventSequenceId("inbox-orders+email+constraint")]));
    }

    async Task Because()
    {
        _closedStreamsResult = await Check("inbox-orders+closed_streams");
        _constraintResult = await Check("inbox-orders+email+constraint");
    }

    [Fact] void should_refuse_the_existing_closed_streams_name() => _closedStreamsResult.ShouldBeFalse();
    [Fact] void should_refuse_the_existing_constraint_name() => _constraintResult.ShouldBeFalse();
    [Fact] void should_consult_the_reservation_provider() => _reservedEventSequenceIds.Received(1).IsReserved(new EventSequenceId("inbox-orders+closed_streams"));
}
