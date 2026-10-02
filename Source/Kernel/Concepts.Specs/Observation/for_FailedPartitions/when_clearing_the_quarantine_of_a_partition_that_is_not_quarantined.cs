// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_clearing_the_quarantine_of_a_partition_that_is_not_quarantined : Specification
{
    static readonly Key _partition = new("some-partition", Properties.ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;
    FailedPartition _failedPartition;
    bool _result;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartition = _failedPartitions.RegisterAttempt(_partition, EventSequenceNumber.First, ["first"], string.Empty);
    }

    void Because() => _result = _failedPartitions.ClearQuarantine(_partition);

    [Fact] void should_report_that_nothing_changed() => _result.ShouldBeFalse();
    [Fact] void should_not_reset_the_retry_budget() => _failedPartition.AttemptsInCurrentBudget.ShouldEqual(1);
}
