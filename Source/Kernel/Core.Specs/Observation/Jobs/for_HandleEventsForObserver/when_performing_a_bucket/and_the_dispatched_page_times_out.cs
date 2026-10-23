// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing_a_bucket;

public class and_the_dispatched_page_times_out : given.a_page_job_step
{
    Catch<JobStepResult> _result;

    void Establish()
    {
        _performState.Bucket = new(0, 1, 1);
        _pageSubscriber.OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromException<ObserverSubscriberResult>(new TimeoutException("ambiguous timeout")));
    }

    async Task Because() => _result = await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_not_redispatch_the_page() => _pageSubscriber.Received(1).OnNextPage(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_not_bypass_page_dispatch() => _handledBatches.ShouldBeEmpty();
    [Fact] void should_retain_a_transient_partial_result()
    {
        _result.TryGetResult(out var result).ShouldBeTrue();
        result.TryGetFullResult<HandleEventsForPartitionResult>(out var partial, out _, ReplaySerialization.Options()).ShouldBeFalse();
        partial.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
        partial.TransientFailure.ShouldNotBeNull();
    }
}
