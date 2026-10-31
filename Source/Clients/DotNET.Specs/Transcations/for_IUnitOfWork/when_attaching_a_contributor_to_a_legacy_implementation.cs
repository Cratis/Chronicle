// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_IUnitOfWork;

public class when_attaching_a_contributor_to_a_legacy_implementation : Specification
{
    IUnitOfWork _unitOfWork;
    Exception _error;

    void Establish() => _unitOfWork = new when_enrolling_a_batch_with_legacy_implementation.LegacyUnitOfWork();

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEnrollmentContributor(Substitute.For<IUnitOfWorkEnrollmentContributor>()));

    [Fact] void should_fail_loudly() => _error.ShouldBeOfExactType<UnitOfWorkEnrollmentContributorsNotSupported>();
}
