// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_a_failing_completion_callback : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    Exception _callbackError;
    Exception _disposalError;
    Exception _appendError;

    async Task Because()
    {
        var refusal = Refuse();
        _callbackError = new Exception("completion callback failed");
        _unit.OnCompleted(_ => throw _callbackError);
        await _sequence.Transactional.Append("source", "changed");
        ClearCalls();
        _disposalError = Catch.Exception(refusal.Dispose);
        _appendError = await Catch.Exception(() => _sequence.Append("source", "allowed"));
    }

    [Fact] void should_preserve_the_callback_failure() => _disposalError.ShouldEqual(_callbackError);
    [Fact] void should_roll_back_the_unit() => _unit.IsCompleted.ShouldBeTrue();
    [Fact] void should_discard_the_events() => _unit.GetEvents().ShouldBeEmpty();
    [Fact] void should_restore_admission_in_finally() => _appendError.ShouldBeNull();
    [Fact] void should_only_send_the_append_after_disposal() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
}
