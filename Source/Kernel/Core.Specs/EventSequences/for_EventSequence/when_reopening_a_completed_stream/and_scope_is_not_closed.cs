// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_reopening_a_completed_stream;

public class and_scope_is_not_closed : given.a_repairable_sequence
{
    async Task Because() => _result = await _eventSequence.ReopenCompletedStream(_scope, "Repair", CorrelationId.NotSet, [], _actor);

    [Fact] void should_reject_the_repair() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_not_completed() => _result.AsT1.ShouldEqual(ReopenStreamScopeError.NotCompleted);
    [Fact] void should_not_audit_a_rejected_repair() => _systemSequence.ReceivedCalls().ShouldBeEmpty();
}
