// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_scope_only_batch_conflicts : given.a_contributed_unit
{
    Exception _error;

    void Establish() => Stage();

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(EventSequenceId.Log, [], [new(_source, ConcurrencyScope.None)]));

    [Fact] void should_refuse_to_overwrite_the_guard() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_not_consult_the_contributor_for_the_scope_only_entry() => _contributor.Received(1).GetRequiredScope(Arg.Any<UnitOfWorkEnrollment>());
}
