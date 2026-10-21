// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor.given;

public class a_mutation_probe : a_contributed_unit
{
    protected List<Exception> _mutationErrors = [];

    protected void ProbeMutations()
    {
        Action[] mutations =
        [
            () => Stage(),
            () => _unitOfWork.AddEvents(EventSequenceId.Log, [], []),
            () => _unitOfWork.AddDecisionRead(Substitute.For<IDecisionRead>()),
            () => _unitOfWork.AddEnrollmentContributor(_contributor),
            _registration.Dispose,
            () => _unitOfWork.Commit().GetAwaiter().GetResult(),
            () => _unitOfWork.Rollback().GetAwaiter().GetResult(),
            _unitOfWork.Dispose,
            () => _unitOfWork.ClaimDecisionReadCommitOwnership(),
            () => _unitOfWork.OnCompleted(_ => { })
        ];
        _mutationErrors.AddRange(mutations.Select(_ => Catch.Exception(_)));
    }
}
