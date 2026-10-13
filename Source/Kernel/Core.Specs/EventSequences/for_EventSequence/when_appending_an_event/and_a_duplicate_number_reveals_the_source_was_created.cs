// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_a_duplicate_number_reveals_the_source_was_created : given.a_before_first_append_retry
{
    AppendResult _result;

    void Establish() => _retryTail = 5;

    async Task Because() => _result = await AppendSingleWithScope();

    [Fact] void should_reject_the_append() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_freshness_violation() => _result.ConcurrencyViolation.ShouldEqual(new ConcurrencyViolation(_eventSourceId, EventSequenceNumber.BeforeFirst, 5));
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_check_freshness_again() => _scopeChecks.ShouldEqual(2);
    [Fact] void should_not_write_again() => _attempts.ShouldEqual(1);
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
