// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling.with_an_enrollment_contributor;

public class and_a_later_batch_conflicts : given.a_contributed_unit
{
    Exception _error;

    void Establish()
    {
        Stage();
        _registration.Dispose();
    }

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(EventSequenceId.Log, [new(_source, new object())], [new(_source, ConcurrencyScope.None)]));

    [Fact] void should_keep_the_recorded_guard_after_disposal() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_preserve_only_the_previous_event() => _unitOfWork.GetEvents().ShouldContainOnly(_event);
}
