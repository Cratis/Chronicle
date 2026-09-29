// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing;

/// <summary>
/// The observer subscribes to one generation of an event type while the stream holds events stored at another. An event is
/// released with the compliance metadata of the schema it was stored at, so that schema has to be resolved for it.
/// </summary>
public class and_the_event_is_stored_at_a_later_generation_than_the_subscribed_one : given.a_performing_job_step
{
    static readonly EventType _first_generation = new("some-event", EventTypeGeneration.First);
    static readonly EventType _second_generation = new("some-event", new EventTypeGeneration(2));

    EventTypeSchema _first_generation_schema;
    EventTypeSchema _second_generation_schema;
    Dictionary<EventType, EventTypeSchema> _schemasReleasedWith;

    void Establish()
    {
        _first_generation_schema = new(_first_generation, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema { Description = "first" });
        _second_generation_schema = new(_second_generation, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema { Description = "second" });

        _performState.EventTypes = [_first_generation];
        _eventTypesStorage.GetFor(Arg.Any<IEnumerable<EventType>>())
            .Returns(callInfo => Task.FromResult<IEnumerable<EventTypeSchema>>(
                [.. callInfo.Arg<IEnumerable<EventType>>().Select(_ => _.Generation == _first_generation.Generation ? _first_generation_schema : _second_generation_schema)]));

        var storedEvent = AppendedEvent.EmptyWithEventType(_second_generation) with
        {
            Context = EventContext.Empty with
            {
                EventType = _second_generation,
                EventSourceId = "some-partition",
                SequenceNumber = 1UL
            }
        };
        _eventCursor.Current.Returns([storedEvent]);
        _eventCompliance
            .Release(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<IDictionary<EventType, EventTypeSchema>>())
            .Returns(callInfo =>
            {
                _schemasReleasedWith = new Dictionary<EventType, EventTypeSchema>(callInfo.Arg<IDictionary<EventType, EventTypeSchema>>());
                return Task.FromResult(callInfo.Arg<IEnumerable<AppendedEvent>>().ToArray());
            });
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_release_the_event_with_the_schema_of_the_generation_it_was_stored_at() => _schemasReleasedWith[_second_generation].ShouldEqual(_second_generation_schema);
    [Fact] void should_keep_the_schema_of_the_subscribed_generation() => _schemasReleasedWith[_first_generation].ShouldEqual(_first_generation_schema);
}
