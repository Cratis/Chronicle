// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_deferring_a_partition : Specification
{
    FailedPartitions _partitions;
    FailedPartition _result;
    PartitionDeferral _deferral;

    void Establish()
    {
        _partitions = new();
        var since = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        _deferral = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, since, since.AddMinutes(1), new(42));
    }

    void Because() => _result = _partitions.Defer("partition", _deferral);

    [Fact] void should_record_no_attempts() => _result.Attempts.ShouldBeEmpty();
    [Fact] void should_not_consume_the_retry_budget() => _result.AttemptsInCurrentBudget.ShouldEqual(0);
    [Fact] void should_remember_the_deadline_and_resume_position() => _result.Deferral.ShouldEqual(_deferral);
    [Fact] void should_keep_the_partition_unresolved() => _result.IsResolved.ShouldBeFalse();
    [Fact] void should_block_live_delivery() => _partitions.IsFailed("partition").ShouldBeTrue();
}
