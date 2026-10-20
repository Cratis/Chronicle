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

public class and_a_webhook_job_is_cancelled : given.a_performing_job_step
{
    Catch<JobStepResult> _result;
    int _calls;
    bool _cancelled;

    void Establish()
    {
        _observersConfig = new Observers { SubscriberTimeout = 1 };
        var subscriber = Substitute.For<IWebhookObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                _calls++;
                try
                {
                    await new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task.WaitAsync(call.Arg<CancellationToken>());
                    return ObserverSubscriberResult.Ok(EventSequenceNumber.First);
                }
                finally
                {
                    _cancelled = true;
                }
            });
        _jobStep.SetupForTesting(_observer, subscriber, "some-partition");
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        _result = await _jobStep.InvokePerformStep(_performState, cancellation.Token);
    }

    [Fact] void should_cancel_and_drain_the_delivery() => _cancelled.ShouldBeTrue();
    [Fact] void should_end_the_step_as_cancelled() => _result.Match(result => result.TryGetError(out var error) && error.Cancelled, _ => false).ShouldBeTrue();
    [Fact] void should_not_report_a_partition_failure() => _observer.DidNotReceive().PartitionFailed(
        Arg.Any<Key>(), Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<string>>(), Arg.Any<string>(), Arg.Any<FailureKind>());
}
