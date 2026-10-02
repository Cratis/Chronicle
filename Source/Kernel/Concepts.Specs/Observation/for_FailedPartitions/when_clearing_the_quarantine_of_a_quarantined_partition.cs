// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_clearing_the_quarantine_of_a_quarantined_partition : Specification
{
    static readonly Key _partition = new("some-partition", Properties.ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;
    FailedPartition _failedPartition;
    bool _result;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartition = _failedPartitions.RegisterAttempt(_partition, EventSequenceNumber.First, ["first"], string.Empty);
        _failedPartitions.RegisterAttempt(_partition, EventSequenceNumber.First, ["second"], string.Empty);
        _failedPartitions.Quarantine(_partition);
    }

    void Because() => _result = _failedPartitions.ClearQuarantine(_partition);

    [Fact] void should_report_that_it_was_cleared() => _result.ShouldBeTrue();
    [Fact] void should_no_longer_be_quarantined() => _failedPartition.IsQuarantined.ShouldBeFalse();
    [Fact] void should_keep_the_attempt_history() => _failedPartition.Attempts.Count().ShouldEqual(2);
    [Fact] void should_reset_the_attempts_in_the_current_budget() => _failedPartition.AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_remember_how_many_attempts_came_before_the_reset() => _failedPartition.AttemptsBeforeBudgetReset.ShouldEqual(2);
}
