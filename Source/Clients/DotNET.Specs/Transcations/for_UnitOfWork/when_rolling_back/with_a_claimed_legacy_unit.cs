// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class with_a_claimed_legacy_unit : given.a_unit_of_work
{
    async Task Because()
    {
        _unitOfWork.ClaimDecisionReadCommitOwnership();
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown());
        await _unitOfWork.Rollback();
    }

    [Fact] void should_allow_legacy_rollback() => _unitOfWork.IsCompleted.ShouldBeTrue();
    [Fact] void should_clear_the_events() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_invoke_completion() => _onCompletedCalled.ShouldBeTrue();
}
