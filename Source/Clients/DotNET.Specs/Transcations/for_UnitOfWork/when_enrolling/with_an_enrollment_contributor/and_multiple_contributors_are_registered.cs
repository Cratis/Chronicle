// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_multiple_contributors_are_registered : given.a_contributed_unit
{
    List<int> _order;
    IUnitOfWorkEnrollmentContributor _second;

    void Establish()
    {
        _order = [];
        _contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_ =>
        {
            _order.Add(1);
            return _required;
        });
        _second = Substitute.For<IUnitOfWorkEnrollmentContributor>();
        _second.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_ =>
        {
            _order.Add(2);
            return ConcurrencyScope.NotSet;
        });
        _unitOfWork.AddEnrollmentContributor(_second);
    }

    void Because() => Stage();

    [Fact] void should_consult_in_registration_order() => _order.ShouldEqual(new[] { 1, 2 });
    [Fact] void should_notify_both_with_the_effective_scope() => _second.Received(1).Enrolled(Arg.Any<UnitOfWorkEnrollment>(), _required);
}
