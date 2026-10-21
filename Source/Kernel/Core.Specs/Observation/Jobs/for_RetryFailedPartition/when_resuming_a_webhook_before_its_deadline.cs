// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition;

public class when_resuming_a_webhook_before_its_deadline : given.a_retry_failed_partition_job
{
    bool _canResume;

    void Establish()
    {
        _request = _request with { ObserverType = ObserverType.External };
        _observer.IsSubscribed().Returns(true);
        _observer.GetSubscription().Returns(new ObserverSubscription("observer-id", _request.ObserverKey, [], typeof(IWebhookObserverSubscriber), SiloAddress.Zero));
        _observer.CanResumeFailedPartition(_request.Key, null, null).Returns(false);
        _stateStorage.State.Request = _request;
    }

    async Task Because() => _canResume = await _job.CheckCanResume();

    [Fact] void should_not_resume_before_the_deadline() => _canResume.ShouldBeFalse();
    [Fact] void should_consult_the_webhook_fence() => _observer.Received(1).CanResumeFailedPartition(_request.Key, null, null);
}
