// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_registering_an_attempt_that_failed_earlier_than_before : Specification
{
    static readonly Key _partition = new("some-partition", Properties.ArrayIndexers.NoIndexers);

    FailedPartitions _failedPartitions;
    FailedPartition _failedPartition;

    void Establish()
    {
        _failedPartitions = new();
        _failedPartitions.RegisterAttempt(_partition, 42UL, ["first"], string.Empty, FailureKind.Handling);
    }

    void Because() => _failedPartition = _failedPartitions.RegisterAttempt(_partition, 7UL, ["second"], string.Empty, FailureKind.Handling);

    [Fact] void should_count_the_attempt_against_the_budget() => _failedPartition.AttemptsInCurrentBudget.ShouldEqual(2);
}
