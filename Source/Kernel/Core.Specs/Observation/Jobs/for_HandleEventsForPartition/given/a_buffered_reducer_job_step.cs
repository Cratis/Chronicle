// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Reducers.Clients;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

public class a_buffered_reducer_job_step : a_performing_job_step
{
    protected Guid _window = Guid.NewGuid();
    protected Catch<JobStepResult> _result;

    void Establish()
    {
        var eventType = new EventType("some-event", EventTypeGeneration.First);
        _performState.EventTypes = [eventType];
        _performState.EventObservationState = EventObservationState.Replay;
        _eventCursor.Current.Returns([AppendedEvent.EmptyWithEventType(eventType) with
        {
            Context = EventContext.Empty with { EventType = eventType, SequenceNumber = 1UL }
        }]);
        var reducer = Substitute.For<IReducerObserverSubscriber>();
        reducer.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok(1UL)));
        var subscription = new ObserverSubscription(
            "observer-id", _performState.ObserverKey, _performState.EventTypes, typeof(IReducerObserverSubscriber), SiloAddress.Zero);
        _observer.GetSubscription().Returns(subscription);
        _jobStep.SetupForTesting(_observer, reducer, "some-partition", subscription);
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>())
            .Returns(Task.FromResult(Windows(_window)));
    }

    protected static IReadOnlyDictionary<SiloAddress, Guid[]> Windows(params Guid[] windows) =>
        new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = windows };
}
