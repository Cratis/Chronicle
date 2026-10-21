// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_FailedPartitions;

public class when_deferring_an_existing_partition_again : Specification
{
    FailedPartitions _partitions;
    FailedPartition _existing;
    FailedPartition _result;
    PartitionDeferral _next;

    void Establish()
    {
        _partitions = new();
        _existing = _partitions.RegisterAttempt("partition", new(42), ["failed"], string.Empty);
        var since = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        _partitions.Defer("partition", new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, since, since.AddMinutes(1), new(42)));
        _next = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, since.AddMinutes(1), since.AddMinutes(3), new(43));
    }

    void Because() => _result = _partitions.Defer("partition", _next);

    [Fact] void should_keep_the_episode_identity() => _result.Id.ShouldEqual(_existing.Id);
    [Fact] void should_keep_a_single_entry() => _partitions.Partitions.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_attempt_history() => _result.Attempts.Count().ShouldEqual(1);
    [Fact] void should_replace_the_deferral() => _result.Deferral.ShouldEqual(_next);
}
