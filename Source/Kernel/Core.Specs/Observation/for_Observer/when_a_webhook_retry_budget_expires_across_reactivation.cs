// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer;

public class when_a_webhook_retry_budget_expires_across_reactivation : given.a_webhook_observer
{
    FailedPartition _failure;
    FailedPartitionId _episodeId;
    Guid _deferralId;
    bool _canResume;

    protected override Configuration.Observers CreateWebhookRetryConfig() => new Configuration.Webhooks().Retry;

    async Task Establish()
    {
        _failure = _failedPartitionsState.RegisterAttempt("partition", 42UL, ["503"], "", FailureKind.Handling);
        _failure.LastAttempt.Occurred = DateTimeOffset.UtcNow.AddHours(-25);
        var now = DateTimeOffset.UtcNow;
        _failure.Deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryBackoff, now.AddMinutes(-2), now.AddMinutes(-1), new(42));
        _episodeId = _failure.Id;
        _deferralId = _failure.Deferral.Id;
        await _failedPartitionsStorage.WriteStateAsync();
        await Crash();
        _failure = _failedPartitionsState.Partitions.Single();
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, null, true);
    }

    async Task Because() => _canResume = await _observer.CanResumeFailedPartition("partition", _episodeId, _deferralId);

    [Fact] void should_refuse_delivery_for_the_persisted_episode() => _canResume.ShouldBeFalse();
    [Fact] void should_park_the_partition_without_another_delivery() => _failure.IsQuarantined.ShouldBeTrue();
    [Fact] void should_persist_the_parked_partition() => _failedPartitionsStorage.State.Partitions.Single().IsQuarantined.ShouldBeTrue();
    [Fact] void should_remove_the_expired_deadline() => _failure.Deferral.ShouldBeNull();
    [Fact] void should_not_start_recovery() => _jobsManager.DidNotReceive().Start<IRetryFailedPartition, RetryFailedPartitionRequest>(Arg.Any<RetryFailedPartitionRequest>());
    [Fact] void should_not_burn_an_attempt_on_reactivation() => _failure.AttemptsInCurrentBudget.ShouldEqual(1);
    [Fact] void should_apply_the_default_parked_partition_percentage() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
}
