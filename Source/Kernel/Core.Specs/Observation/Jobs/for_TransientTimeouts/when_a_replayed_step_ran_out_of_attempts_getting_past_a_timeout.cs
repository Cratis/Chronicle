// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_a_replayed_step_ran_out_of_attempts_getting_past_a_timeout : given.a_step_that_ended_with_a_timeout
{
    IObserver _observer;
    ReplayEpoch _epoch;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _epoch = new ReplayEpoch(Guid.NewGuid());
    }

    async Task Because() => await TransientTimeouts.ReportPartitionFailedWhenAttemptsAreExhausted(_observer, _timedOut, _options, _epoch);

    [Fact] void should_report_the_partition_failed_with_the_replay_epoch() =>
        _observer.Received(1).PartitionFailedWithDeliveryFence(
            _partition,
            Arg.Any<Cratis.Chronicle.Concepts.Events.EventSequenceNumber>(),
            Arg.Any<IEnumerable<string>>(),
            string.Empty,
            FailureKind.Timeout,
            Arg.Is<PartitionFailureContext>(c => c.ReplayEpoch == _epoch));
}
