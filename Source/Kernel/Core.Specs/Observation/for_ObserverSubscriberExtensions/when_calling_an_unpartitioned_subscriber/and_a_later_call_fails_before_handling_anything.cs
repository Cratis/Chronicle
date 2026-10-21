// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverSubscriberExtensions.when_calling_an_unpartitioned_subscriber;

public class and_a_later_call_fails_before_handling_anything : given.an_unpartitioned_subscriber
{
    ObserverSubscriberResult _result;

    protected override Task<ObserverSubscriberResult> HandleBatch(Concepts.Events.AppendedEvent[] batch)
    {
        _batchSizes.Add(batch.Length);
        return Task.FromResult(_batchSizes.Count == 2
            ? ObserverSubscriberResult.Failed(Concepts.Events.EventSequenceNumber.Unavailable, "boom")
            : ObserverSubscriberResult.Ok(batch[^1].Context.SequenceNumber));
    }

    async Task Because() => _result = await _subscriber.OnNextWithin(TimeSpan.FromMinutes(1), "some-partition", _events, new ObserverSubscriberContext(null));

    [Fact] void should_stop_after_the_failing_call() => _batchSizes.ShouldEqual([20, 20]);
    [Fact] void should_be_failed() => _result.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_report_the_progress_of_the_calls_before_it() => _result.LastSuccessfulObservation.Value.ShouldEqual(19UL);
    [Fact] void should_keep_the_failure_reason() => _result.ExceptionMessages.ShouldContain("boom");
}
