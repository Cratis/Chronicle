// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_staging_after_completion;

public class and_compatible_unit_rolled_back : given.a_unit_for_late_staging
{
    async Task Because()
    {
        await _unitOfWork.Rollback();
        AttemptLateStaging();
    }

    [Fact] void should_accept_an_event() => _eventError.ShouldBeNull();
    [Fact] void should_accept_a_batch() => _batchError.ShouldBeNull();
    [Fact] void should_stage_both_attempts() => _unitOfWork.GetEvents().Count().ShouldEqual(2);
    [Fact] void should_log_both_attempts_at_error_level() => GetErrorLogs().Length.ShouldEqual(2);
}
