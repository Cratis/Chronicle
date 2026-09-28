// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_staging_after_completion;

public class and_strict_unit_committed : given.a_strict_unit_for_late_staging
{
    async Task Because()
    {
        await _unitOfWork.Commit();
        AttemptLateStaging();
    }

    [Fact] void should_refuse_an_event() => _eventError.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_refuse_a_batch() => _batchError.ShouldBeOfExactType<UnitOfWorkIsCompleted>();
    [Fact] void should_not_enumerate_the_batch() => _batchWasEnumerated.ShouldBeFalse();
    [Fact] void should_leave_staged_events_unchanged() => _unitOfWork.GetEvents().ShouldContainOnly([_original]);
}
