// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_an_enrollment_contributor_requirement : given.a_unit_with_implicit_scopes
{
    readonly ConcurrencyScope _required = new(1, EventStreamType: "items");
    void Establish()
    {
        Guard();
        var contributor = Substitute.For<IUnitOfWorkEnrollmentContributor>();
        contributor.GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>()).Returns(_required);
        _unit.AddEnrollmentContributor(contributor);
        Enroll();
    }
    Task Because() => Commit();
    [Fact] void should_keep_the_contributor_requirement_explicit() => Assert.Equivalent(_required.ToSequencesContract(), ScopeFor(_sourceId));
}
