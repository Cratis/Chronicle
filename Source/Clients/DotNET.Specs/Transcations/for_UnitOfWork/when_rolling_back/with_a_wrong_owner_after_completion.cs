// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_back;

public class with_a_wrong_owner_after_completion : given.a_unit_of_work
{
    DecisionReadCommitOwner _wrongOwner;
    Exception _exception;

    async Task Establish()
    {
        var owner = _unitOfWork.ClaimDecisionReadCommitOwnership();
        using var other = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        _wrongOwner = other.ClaimDecisionReadCommitOwnership();
        await _unitOfWork.RollbackAsOwner(owner);
    }

    async Task Because() => _exception = await Catch.Exception(() => _unitOfWork.RollbackAsOwner(_wrongOwner));

    [Fact] void should_check_ownership_before_completion() => _exception.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_remain_completed() => _unitOfWork.IsCompleted.ShouldBeTrue();
}
