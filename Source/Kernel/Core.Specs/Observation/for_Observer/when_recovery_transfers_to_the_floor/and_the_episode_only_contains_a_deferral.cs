// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovery_transfers_to_the_floor;

public class and_the_episode_only_contains_a_deferral : given.an_observer_with_a_handover_log
{
    RetryFailedPartitionRequest _retry;
    EventSequenceNumber _beforeRecovery;
    EventSequenceNumber _acceptedFloor;
    bool _deliveredBeforeRecovery;

    void Establish() => _jobsManager.Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>())
        .Returns(call =>
        {
            _retry = call.Arg<RetryFailedPartitionRequest>();
            return Result<JobId, StartJobError>.Success(JobId.New());
        });

    async Task Because()
    {
        Append("P", 11UL);
        var now = DateTimeOffset.UtcNow;
        await _observer.PartitionDeferred("P", new(Guid.NewGuid(), PartitionDeferralReason.RetryAfter, now.AddMinutes(-2), now.AddMinutes(-1), 11UL));
        var episode = _failedPartitionsStorage.State.Partitions.Single();
        await _observer.ReceiveReminder(Observer.PartitionReminderName(episode.Id), default);
        var exhaustedAt = await DeliverRange(_retry.FromSequenceNumber, EventSequenceNumber.Max, [event_type], "P");

        await _observer.Handle("P", [Append("P", 12UL)]);
        await _observer.Handle("Q", [Append("Q", 13UL)]);
        _beforeRecovery = (await _observer.GetState()).NextEventSequenceNumber;
        _deliveredBeforeRecovery = _delivered.Contains(12UL);

        await _observer.FailedPartitionRecovered("P", exhaustedAt, exhaustedAt, _retry.EpisodeId, _retry.DeferralId);
        _acceptedFloor = PersistedObserverState.NextEventSequenceNumber;
        await _silo.RunCatchUpContinuation();
        await _observer.Handle("Q", [Append("Q", 14UL)]);
    }

    [Fact] void should_retry_from_the_deferral() => _retry.FromSequenceNumber.ShouldEqual((EventSequenceNumber)11UL);
    [Fact] void should_have_skipped_the_post_read_append_before_recovery() => _deliveredBeforeRecovery.ShouldBeFalse();
    [Fact] void should_have_advanced_past_the_post_read_append_before_recovery() => _beforeRecovery.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_confirm_the_deferrals_resume_position_before_releasing_the_episode() => _acceptedFloor.ShouldEqual((EventSequenceNumber)11UL);
    [Fact] void should_deliver_the_post_read_append() => _delivered.ShouldContain((EventSequenceNumber)12UL);
    [Fact] void should_finish_recovery() => _failedPartitionsStorage.State.IsFailed("P").ShouldBeFalse();
    [Fact] void should_deliver_a_later_live_probe() => _delivered.ShouldContain((EventSequenceNumber)14UL);
}
