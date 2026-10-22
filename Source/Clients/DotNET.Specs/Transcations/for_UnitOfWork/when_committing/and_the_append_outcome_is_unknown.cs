// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_the_append_outcome_is_unknown : given.a_unit_of_work_with_two_events_for_different_event_source_ids_added_to_it
{
    protected override AppendManyResult GetAppendResult() => new()
    {
        CorrelationId = _correlationId,
        Errors = [AppendError.AppendOutcomeUnknown]
    };

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_report_the_typed_error() => _unitOfWork.GetAppendErrors().ShouldContainOnly([AppendError.AppendOutcomeUnknown]);
    [Fact] void should_not_claim_success() => _unitOfWork.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_report_a_constraint_rejection() => _unitOfWork.GetConstraintViolations().ShouldBeEmpty();
}
