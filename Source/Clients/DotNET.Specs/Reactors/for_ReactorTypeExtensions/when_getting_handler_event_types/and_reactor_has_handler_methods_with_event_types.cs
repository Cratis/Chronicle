// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_handler_event_types;

public class and_reactor_has_handler_methods_with_event_types : Specification
{
    [EventType]
    class FirstEvent;

    [EventType]
    class SecondEvent;

    class ReactorWithTwoEventTypes : IReactor
    {
        public Task Handle(FirstEvent @event) => Task.CompletedTask;
        public Task Handle(SecondEvent @event) => Task.CompletedTask;
    }

    IEnumerable<Type> _result;

    void Because() => _result = typeof(ReactorWithTwoEventTypes).GetHandlerEventTypes();

    [Fact] void should_return_all_event_types() => _result.Count().ShouldEqual(2);
    [Fact] void should_include_first_event_type() => _result.ShouldContain(typeof(FirstEvent));
    [Fact] void should_include_second_event_type() => _result.ShouldContain(typeof(SecondEvent));
}
