// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_recovering_a_replayed_webhook_partition : given.a_webhook_observer
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000008"));
    FailedPartition _failure;

    void Establish()
    {
        var now = DateTimeOffset.UtcNow;
        var deferral = new PartitionDeferral(Guid.NewGuid(), PartitionDeferralReason.CircuitOpen, now.AddMinutes(-2), now.AddMinutes(-1), new(42));
        _failure = _failedPartitionsState.Defer("partition", deferral);
        _failure.ReplayEpoch = _epoch;
    }

    Task Because() => _observer.ReceiveReminder(Observer.PartitionReminderName(_failure.Id), default);

    [Fact] void should_start_the_recovery_with_the_replay_epoch() => _jobsManager.Received(1).Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Is<RetryFailedPartitionRequest>(_ => _.ReplayEpoch == _epoch));
}
