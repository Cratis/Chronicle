// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_TransientTimeouts;

public class when_a_controlled_replay_exhausts_timeout_attempts : given.a_step_that_ended_with_a_timeout
{
    IObserver _observer;
    PartitionFailureContext _context;

    void Establish()
    {
        _observer = Substitute.For<IObserver>();
        _context = new()
        {
            EpisodeId = FailedPartitionId.New(),
            DeferralId = Guid.NewGuid(),
            RecoveryCursor = new(42UL, FailedPartitionRecoveryMode.Replay, 100UL)
        };
        _timedOut = JobStepResult.Failed(PerformJobStepError.FailedWithPartialResult(
            new HandleEventsForPartitionResult(EventSequenceNumber.Unavailable)
            {
                TransientFailure = new(_partition, 42UL, ["Response did not arrive on time"]) { RecoveryContext = _context }
            },
            ["Response did not arrive on time"],
            string.Empty));
    }

    Task Because() => TransientTimeouts.ReportPartitionFailedWhenAttemptsAreExhausted(_observer, _timedOut, _options);

    [Fact] void should_forward_the_episode_deferral_and_replay_cursor() => _observer.Received(1).PartitionFailedWithDeliveryFence(
        _partition, (EventSequenceNumber)42UL, Arg.Any<IEnumerable<string>>(), string.Empty, FailureKind.Timeout, _context);
}
