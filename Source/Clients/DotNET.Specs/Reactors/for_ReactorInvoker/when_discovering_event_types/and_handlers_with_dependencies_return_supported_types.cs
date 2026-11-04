// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_event_types;

public class and_handlers_with_dependencies_return_supported_types : Specification
{
    static readonly Type[] _types = [typeof(VoidEvent), typeof(TaskEvent), typeof(TaskOfEvent), typeof(EventReturned), typeof(SequenceEvent), typeof(AppendEvent), typeof(ReturnedEvent), typeof(ElementEvent), typeof(AppendedEvent)];

    IEventTypes _eventTypes;
    Exception _error;
    IEnumerable<EventType> _result;

    void Establish() => _eventTypes = new EventTypesForSpecifications(_types);

    void Because() => _error = Catch.Exception(() => _result = ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(SupportedReactor)));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_discover_all_event_types() => _result.ShouldContainOnly(
        _eventTypes.GetEventTypeFor(typeof(VoidEvent)),
        _eventTypes.GetEventTypeFor(typeof(TaskEvent)),
        _eventTypes.GetEventTypeFor(typeof(TaskOfEvent)),
        _eventTypes.GetEventTypeFor(typeof(EventReturned)),
        _eventTypes.GetEventTypeFor(typeof(SequenceEvent)),
        _eventTypes.GetEventTypeFor(typeof(AppendEvent)));

    public interface IService;

    [EventType]
    public record VoidEvent;

    [EventType]
    public record TaskEvent;

    [EventType]
    public record TaskOfEvent;

    [EventType]
    public record EventReturned;

    [EventType]
    public record SequenceEvent;

    [EventType]
    public record AppendEvent;

    [EventType]
    public record ReturnedEvent;

    [EventType]
    public record ElementEvent;

    [EventType]
    public record AppendedEvent;

    public record Unclaimed;

    public class SupportedReactor
    {
        public void ReturnsVoid(VoidEvent @event, IService service)
        {
        }

        public Task ReturnsTask(TaskEvent @event, IService service) => Task.CompletedTask;

        public Task<Unclaimed> ReturnsTaskOfUnclaimed(TaskOfEvent @event, IService service) => Task.FromResult(new Unclaimed());

        public ReturnedEvent ReturnsEvent(EventReturned @event, IService service) => new();

        public IEnumerable<ElementEvent> ReturnsEvents(SequenceEvent @event, IService service) => [];

        public EventForEventSourceId ReturnsEventForEventSourceId(AppendEvent @event, IService service) => new(EventSourceId.New(), new AppendedEvent());
    }
}
