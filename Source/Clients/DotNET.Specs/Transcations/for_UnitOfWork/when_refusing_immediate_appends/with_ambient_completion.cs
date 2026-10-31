// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_ambient_completion : EventSequences.for_EventSequence.when_immediate_appends_are_refused.given.a_unit_with_native_sequence
{
    bool _clearedAmbient;
    Exception _replacementError;
    Exception _afterDisposalError;

    async Task Because()
    {
        var owner = Refuse();
        try
        {
            ClearCalls();
            await owner.Rollback();
            _clearedAmbient = !_manager.HasCurrent;
            _error = await Catch.Exception(() => _sequence.Append("source", "escaped"));
            _replacementError = Catch.Exception(() => _manager.Begin(CorrelationId.New()));
        }
        finally
        {
            owner.Dispose();
        }
        _afterDisposalError = await Catch.Exception(() => _sequence.Append("source", "allowed"));
    }

    [Fact] void should_clear_the_completed_ambient_unit() => _clearedAmbient.ShouldBeTrue();
    [Fact] void should_keep_refusal_after_ambient_clear() => _error.ShouldBeOfExactType<ImmediateAppendRefused>();
    [Fact] void should_not_allow_replacement_after_ambient_clear() => _replacementError.ShouldBeOfExactType<ImmediateAppendRefusalRequiresOwner>();
    [Fact] void should_restore_admission_only_on_owner_disposal() => _afterDisposalError.ShouldBeNull();
    [Fact] void should_only_send_the_post_disposal_append() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
}
