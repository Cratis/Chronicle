// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_a_duplicate_number_advances_the_scope : given.a_batch_append_retry
{
    void Establish() => _retryTail = 5;

    async Task Because() => _result = await AppendWithScope();

    [Fact] void should_reject_the_batch() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_return_the_concurrency_violation() => _result.ConcurrencyViolations.ShouldContainOnly(new ConcurrencyViolation(_readSource, 3, 5));
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_check_the_scope_again() => _scopeChecks.ShouldEqual(2);
    [Fact] void should_not_write_again() => _attempts.ShouldEqual(1);
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
