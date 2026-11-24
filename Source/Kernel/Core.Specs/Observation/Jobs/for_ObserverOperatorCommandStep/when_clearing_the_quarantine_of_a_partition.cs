// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_ObserverOperatorCommandStep;

public class when_clearing_the_quarantine_of_a_partition : given.an_observer_to_give_commands_to
{
    Exception _clearedAndRetried;
    Exception _notQuarantined;
    Exception _notFound;
    Exception _clearedButNotRetried;

    async Task Because()
    {
        _observer.ClearPartitionQuarantine("quarantined", true).Returns(new ClearPartitionQuarantineResult(ClearPartitionQuarantineOutcome.Cleared, PartitionRecoveryOutcome.Started));
        _clearedAndRetried = await Catch.Exception(() => Give(ObserverOperatorCommand.ClearPartitionQuarantine, "quarantined", true));

        _observer.ClearPartitionQuarantine("failed", false).Returns(new ClearPartitionQuarantineResult(ClearPartitionQuarantineOutcome.NotQuarantined, PartitionRecoveryOutcome.Started));
        _notQuarantined = await Catch.Exception(() => Give(ObserverOperatorCommand.ClearPartitionQuarantine, "failed"));

        _observer.ClearPartitionQuarantine("healthy", false).Returns(new ClearPartitionQuarantineResult(ClearPartitionQuarantineOutcome.NotFound, PartitionRecoveryOutcome.Started));
        _notFound = await Catch.Exception(() => Give(ObserverOperatorCommand.ClearPartitionQuarantine, "healthy"));

        _observer.ClearPartitionQuarantine("held", true).Returns(new ClearPartitionQuarantineResult(ClearPartitionQuarantineOutcome.Cleared, PartitionRecoveryOutcome.ObserverQuarantined));
        _clearedButNotRetried = await Catch.Exception(() => Give(ObserverOperatorCommand.ClearPartitionQuarantine, "held", true));
    }

    [Fact] void should_succeed_when_the_quarantine_was_cleared_and_the_partition_retried() => _clearedAndRetried.ShouldBeNull();
    [Fact] void should_fail_when_the_partition_is_not_quarantined() => _notQuarantined.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_when_the_partition_is_not_failed() => _notFound.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_fail_when_the_quarantine_was_cleared_but_the_partition_could_not_be_retried() => _clearedButNotRetried.Message.ShouldContain(nameof(PartitionRecoveryOutcome.ObserverQuarantined));
}
