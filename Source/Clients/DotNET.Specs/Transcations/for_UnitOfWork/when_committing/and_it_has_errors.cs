// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_it_has_errors : given.a_unit_of_work_with_two_events_for_different_event_source_ids_added_to_it
{
    protected override AppendManyResult GetAppendResult() => new()
    {
        CorrelationId = _correlationId,
        Errors =
            [
                new AppendError("some error"),
                new AppendError("some other error")
            ]
    };

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_call_on_completed() => _onCompletedCalled.ShouldBeTrue();
    [Fact] void should_have_errors() => _unitOfWork.GetAppendErrors().ShouldContainOnly(_appendResult.Errors);
    [Fact] void should_not_be_successful() => _unitOfWork.IsSuccess.ShouldBeFalse();
}
