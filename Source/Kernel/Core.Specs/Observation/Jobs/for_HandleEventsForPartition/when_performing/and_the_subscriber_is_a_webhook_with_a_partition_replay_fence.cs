// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_subscriber_is_a_webhook_with_a_partition_replay_fence : given.a_performing_job_step_with_observer_filters
{
    IWebhookObserverSubscriber _webhook;

    async Task Establish()
    {
        var subscription = (await _observer.GetSubscription()) with { SubscriberType = typeof(IWebhookObserverSubscriber) };
        _webhook = Substitute.For<IWebhookObserverSubscriber>();
        _webhook.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(ObserverSubscriberResult.Ok(1UL));
        _observer.GetSubscription().Returns(subscription);
        _jobStep.SetupForTesting(_observer, _webhook, "some-partition", subscription);
        _eventCursor.Current.Returns([CreateEvent(1UL, _audited)]);
        _observer.IsPartitionFenced(Arg.Any<Key>(), Arg.Any<JobId>()).Returns(true);
    }

    Task Because() => PerformStep();

    [Fact] void should_deliver_the_webhook_batch() => _webhook.Received(1).OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_check_the_replay_fence() => _observer.DidNotReceive().IsPartitionFenced(Arg.Any<Key>(), Arg.Any<JobId>());
    [Fact] void should_finish_successfully() => _succeeded.ShouldBeTrue();
}
