// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_a_webhook_retry_step_exceeds_the_subscriber_timeout : given.a_performing_job_step
{
    Catch<JobStepResult> _result;
    readonly FailedPartitionId _episodeId = FailedPartitionId.New();
    readonly Guid _deferralId = Guid.NewGuid();

    void Establish()
    {
        _observersConfig = new Observers { SubscriberTimeout = 1 };
        var subscriber = Substitute.For<IWebhookObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(call.Arg<CancellationToken>());
                return ObserverSubscriberResult.Ok(EventSequenceNumber.First);
            });
        _performState.EpisodeId = _episodeId;
        _performState.DeferralId = _deferralId;
        _jobStep.SetupForTesting(_observer, subscriber, "some-partition");
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_report_the_timeout_for_the_current_episode_and_deferral() => _observer.Received(1).PartitionFailed("some-partition", 42UL, Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), FailureKind.Timeout, _episodeId, _deferralId, WebhookDeliveryFailurePolicy.Retry);

    [Fact]
    void should_not_ask_the_job_to_run_the_step_again()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        TransientTimeouts.TryGetTransientFailure(result, new System.Text.Json.JsonSerializerOptions(), out _).ShouldBeFalse();
    }
}
