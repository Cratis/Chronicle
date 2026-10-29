// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_registering_an_attempt_that_advanced_the_partition : Specification
{
    static readonly Key _partition = new("some-partition", Properties.ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;
    FailedPartition _failedPartition;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartitions.RegisterAttempt(_partition, 10UL, ["first"], string.Empty, FailureKind.Timeout);
        _failedPartitions.RegisterAttempt(_partition, 10UL, ["second"], string.Empty, FailureKind.Timeout);
    }

    void Because() => _failedPartition = _failedPartitions.RegisterAttempt(_partition, 310UL, ["third"], string.Empty, FailureKind.Timeout);

    [Fact] void should_keep_the_attempt_history() => _failedPartition.Attempts.Count().ShouldEqual(3);
    [Fact] void should_start_a_new_retry_budget_with_this_attempt() => _failedPartition.AttemptsInCurrentBudget.ShouldEqual(1);
}
