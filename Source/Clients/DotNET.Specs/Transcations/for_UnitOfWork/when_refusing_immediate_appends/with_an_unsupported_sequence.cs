// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_refusing_immediate_appends;

public class with_an_unsupported_sequence : for_UnitOfWork.given.a_unit_of_work
{
    Exception _error;

    async Task Because()
    {
        using var owner = _unitOfWork.RefuseImmediateAppends();
        _unitOfWork.AddEvents(EventSequenceId.Log, [new("source", "changed")], []);
        _eventSequence.ClearReceivedCalls();
        _error = await Catch.Exception(owner.Commit);
    }

    [Fact] void should_fail_instead_of_using_a_public_append() => _error.ShouldBeOfExactType<EventSequenceImmediateAppendRefusalNotSupported>();
    [Fact] void should_not_call_the_untrusted_sequence() => _eventSequence.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_report_success() => _unitOfWork.IsSuccess.ShouldBeFalse();
}
