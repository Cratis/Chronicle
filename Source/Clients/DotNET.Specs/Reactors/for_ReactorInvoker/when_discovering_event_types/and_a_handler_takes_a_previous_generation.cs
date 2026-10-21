// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorInvoker.when_discovering_event_types;

public class and_a_handler_takes_a_previous_generation : Specification
{
    IImmutableList<EventType> _eventTypes;

    void Because() => _eventTypes = ReactorInvoker.GetEventTypesFor(new EventTypesForSpecifications([typeof(Previous), typeof(Current)]), typeof(Reactor));

    [Fact] void should_not_register_the_handler() => _eventTypes.ShouldBeEmpty();

    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);

    [EventType(generation: 2)]
    public record Current(string Name);

    public class Reactor : IReactor
    {
        public Task Handle(Previous @event) => Task.CompletedTask;
    }
}
