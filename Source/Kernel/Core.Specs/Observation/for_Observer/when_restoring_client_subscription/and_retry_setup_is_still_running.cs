// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_retry_setup_is_still_running : given.an_observer_with_failed_client_restoration
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<IImmutableList<JobState>> _jobs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _knownDuringRetry;
    bool _establishedDuringRetry;

    void Establish() => _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(_ =>
    {
        _entered.TrySetResult();
        return _jobs.Task;
    });

    async Task Because()
    {
        var retry = Restore();
        await _entered.Task;
        _knownDuringRetry = await _observer.KnowsClient(_client.ConnectionId);
        _establishedDuringRetry = await _observer.IsSubscriptionEstablished();
        _jobs.SetResult(ImmutableList<JobState>.Empty);
        await retry;
    }

    [Fact] void should_keep_membership_during_setup() => _knownDuringRetry.ShouldBeTrue();
    [Fact] void should_not_claim_establishment_during_setup() => _establishedDuringRetry.ShouldBeFalse();
    [Fact] async Task should_be_established_after_setup_finishes() => (await _observer.IsSubscriptionEstablished()).ShouldBeTrue();
    [Fact] void should_allow_interleaved_health_reads() => Attribute.IsDefined(typeof(IObserver).GetMethod(nameof(IObserver.IsSubscriptionEstablished))!, typeof(AlwaysInterleaveAttribute)).ShouldBeTrue();
    [Fact] void should_not_interleave_restoration() => Attribute.IsDefined(typeof(IObserver).GetMethod(nameof(IObserver.RestoreClientSubscription))!, typeof(AlwaysInterleaveAttribute)).ShouldBeFalse();
}
