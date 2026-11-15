// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_step_belongs_to_a_replay : given.a_performing_job_step
{
    static readonly ReplayEpoch _epoch = new(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003"));
    IWebhookObserverSubscriber _subscriber;
    ObserverSubscriberContext _context;

    void Establish()
    {
        _performState.EventObservationState = EventObservationState.Replay;
        _performState.ReplayEpoch = _epoch;
        _eventCursor.Current.Returns([AppendedEvent.EmptyWithEventSequenceNumber(42)]);
        _subscriber = Substitute.For<IWebhookObserverSubscriber>();
        _subscriber.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Do<ObserverSubscriberContext>(_ => _context = _), Arg.Any<CancellationToken>())
            .Returns(ObserverSubscriberResult.Ok(new(42)));
        _jobStep.SetupForTesting(_observer, _subscriber, "some-partition");
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_give_the_subscriber_the_replay_epoch() => _context.ReplayEpoch.ShouldEqual(_epoch);
}
