// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_an_old_recovery_completes_after_a_new_deferral : given.a_webhook_observer
{
    FailedPartition _failure;
    Guid _oldId;
    PartitionDeferral _current;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        _oldId = Guid.NewGuid();
        _current = new(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now, now.AddMinutes(1), new(42));
        _failure = _failedPartitionsState.Defer("partition", _current);
    }

    Task Because() => _observer.FailedPartitionRecovered("partition", new(42), EventSequenceNumber.Unavailable, _failure.Id, _oldId);

    [Fact] void should_leave_the_partition_unresolved() => _failedPartitionsState.IsFailed("partition").ShouldBeTrue();
    [Fact] void should_keep_the_new_deadline() => _failure.Deferral.ShouldEqual(_current);
}
