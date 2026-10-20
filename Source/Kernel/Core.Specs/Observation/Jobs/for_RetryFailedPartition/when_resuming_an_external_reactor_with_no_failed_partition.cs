// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_RetryFailedPartition;

public class when_resuming_an_external_reactor_with_no_failed_partition : given.a_retry_failed_partition_job
{
    bool _canResume;

    async Task Establish()
    {
        _request = _request with { ObserverType = ObserverType.External };
        _observer.IsSubscribed().Returns(true);
        _observer.GetSubscription().Returns(new ObserverSubscription("observer-id", _request.ObserverKey, [], typeof(IObserverSubscriber), SiloAddress.Zero));
        _observer.CanResumeFailedPartition(_request.Key, null, null).Returns(false);
        await _job.Start(_request);
    }

    async Task Because() => _canResume = await _job.CheckCanResume();

    [Fact] void should_still_resume_the_legacy_reactor_job() => _canResume.ShouldBeTrue();
    [Fact] void should_not_consult_the_webhook_fence() => _observer.DidNotReceive().CanResumeFailedPartition(_request.Key, null, null);
}
