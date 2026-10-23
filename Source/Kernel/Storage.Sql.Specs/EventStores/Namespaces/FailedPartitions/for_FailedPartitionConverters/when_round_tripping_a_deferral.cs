// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions.for_FailedPartitionConverters;

public class when_round_tripping_a_deferral : Specification
{
    PartitionDeferral _expected;
    Concepts.Observation.FailedPartition _restored;

    void Establish()
    {
        var since = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        _expected = new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, since, since.AddMinutes(3), new(42));
    }

    void Because() => _restored = new Concepts.Observation.FailedPartition { Deferral = _expected }.ToEntity().ToFailedPartition();

    [Fact] void should_keep_the_whole_deferral() => _restored.Deferral.ShouldEqual(_expected);
    [Fact] void should_not_invent_attempts() => _restored.Attempts.ShouldBeEmpty();
}
