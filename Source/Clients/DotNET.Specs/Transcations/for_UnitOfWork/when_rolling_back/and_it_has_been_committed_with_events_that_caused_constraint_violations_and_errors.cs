// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_rolling_Back;

public class and_it_has_been_committed_with_events_that_caused_constraint_violations_and_errors : given.a_unit_of_work_and_events_and_constraint_violations_and_errors
{
    async Task Because() => await _unitOfWork.Rollback();

    [Fact] void should_not_have_events_left_in_unit_of_work() => _unitOfWork.GetEvents().ShouldBeEmpty();
    [Fact] void should_call_on_completed() => _onCompletedCalled.ShouldBeTrue();
}
