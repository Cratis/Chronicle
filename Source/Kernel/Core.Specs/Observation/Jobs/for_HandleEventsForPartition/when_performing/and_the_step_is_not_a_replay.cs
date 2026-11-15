// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_step_is_not_a_replay : given.a_performing_job_step
{
    ObserverSubscriberContext _context;

    void Establish()
    {
        _eventCursor.Current.Returns([AppendedEvent.EmptyWithEventSequenceNumber(42)]);
        var subscriber = Substitute.For<IWebhookObserverSubscriber>();
        subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Do<ObserverSubscriberContext>(_ => _context = _), Arg.Any<CancellationToken>())
            .Returns(ObserverSubscriberResult.Ok(new(42)));
        _jobStep.SetupForTesting(_observer, subscriber, "some-partition");
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_give_the_subscriber_no_replay_epoch() => _context.ReplayEpoch.ShouldBeNull();
}
