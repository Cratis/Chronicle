// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_an_ordered_unit_rejects_a_conflict : given.a_contributed_unit
{
    Exception _error;

    void Establish()
    {
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(_source, _event)], []);
        _registration.Dispose();
    }

    void Because() => _error = Catch.Exception(() => Stage(ConcurrencyScope.None));

    [Fact] void should_reject_the_conflicting_strict_append() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_keep_only_the_batch_event() => _unitOfWork.GetEvents().ShouldContainOnly(_event);
}
