// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartition;

public class when_adding_an_attempt_after_the_budget_was_reset : Specification
{
    FailedPartition _failedPartition;

    void Establish()
    {
        _failedPartition = new();
        _failedPartition.AddAttempt(new() { SequenceNumber = EventSequenceNumber.First });
        _failedPartition.AddAttempt(new() { SequenceNumber = EventSequenceNumber.First });
        _failedPartition.AttemptsBeforeBudgetReset = 2;
    }

    void Because() => _failedPartition.AddAttempt(new() { SequenceNumber = EventSequenceNumber.First });

    [Fact] void should_count_only_the_attempt_since_the_reset() => _failedPartition.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_keep_every_attempt_in_the_history() => _failedPartition.Attempts.Count().ShouldEqual(3);
}
