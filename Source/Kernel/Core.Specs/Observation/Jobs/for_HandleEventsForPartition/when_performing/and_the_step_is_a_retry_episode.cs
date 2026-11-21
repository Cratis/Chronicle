// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition.when_performing;

public class and_the_step_is_a_retry_episode : given.a_performing_job_step
{
    ObserverSubscriberContext _context;

    void Establish()
    {
        _performState.EpisodeId = new(Guid.NewGuid());
        _observerSubscriber
            .OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Do<ObserverSubscriberContext>(context => _context = context))
            .Returns(Task.FromResult(ObserverSubscriberResult.Ok(first_event_sequence_number)));
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_deliver_the_events_as_a_retry() => _context.Origin.ShouldEqual(ObservationOrigin.Retry);
}
