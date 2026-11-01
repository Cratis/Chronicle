// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.given;

public class a_buffered_projection_job_step : a_performing_job_step
{
    protected Guid _window = Guid.NewGuid();
    protected ICollapsingProjectionObserverSubscriber _projection;
    protected Catch<JobStepResult> _result;

    async Task Establish()
    {
        var eventType = new EventType("some-event", EventTypeGeneration.First);
        _performState.EventTypes = [eventType];
        _eventCursor.Current.Returns([AppendedEvent.EmptyWithEventType(eventType) with
        {
            Context = EventContext.Empty with { EventType = eventType, SequenceNumber = 1UL }
        }]);
        _projection = Substitute.For<ICollapsingProjectionObserverSubscriber>();
        _silo.AddProbe(_ => _projection);
        _observer.GetSubscription().Returns(new ObserverSubscription(
            "observer-id", _performState.ObserverKey, _performState.EventTypes, typeof(ICollapsingProjectionObserverSubscriber), SiloAddress.Zero));
        _projection.OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>())
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok(1UL)));
        _performState.EventObservationState = EventObservationState.Replay;
        _observerServiceClient.FlushAllFor(Arg.Any<ObserverDetails>(), Arg.Any<bool>())
            .Returns(_ => Task.FromResult(Windows(_window)));
        await _jobStep.Prepare(new HandleEventsForPartitionArguments(
            _performState.ObserverKey,
            ObserverType.Projection,
            "some-partition",
            0UL,
            ulong.MaxValue,
            EventObservationState.Replay,
            _performState.EventTypes));
    }

    protected static IReadOnlyDictionary<SiloAddress, Guid[]> Windows(params Guid[] windows) =>
        new Dictionary<SiloAddress, Guid[]> { [SiloAddress.Zero] = windows };
}
