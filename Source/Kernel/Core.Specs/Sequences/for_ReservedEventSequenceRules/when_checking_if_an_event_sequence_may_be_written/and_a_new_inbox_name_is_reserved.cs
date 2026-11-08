// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_ReservedEventSequenceRules.when_checking_if_an_event_sequence_may_be_written;

public class and_a_new_inbox_name_is_reserved : given.a_reserved_name_check
{
    bool _closedStreamsResult;
    bool _constraintResult;

    void Establish()
    {
        Reserve("inbox-orders+closed_streams");
        Reserve("inbox-orders+email+constraint");
    }

    async Task Because()
    {
        _closedStreamsResult = await Check("inbox-orders+closed_streams");
        _constraintResult = await Check("inbox-orders+email+constraint");
    }

    [Fact] void should_refuse_the_closed_streams_name() => _closedStreamsResult.ShouldBeFalse();
    [Fact] void should_refuse_the_constraint_name() => _constraintResult.ShouldBeFalse();
}
