// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorTypeExtensions.when_getting_handler_event_types;

public class and_same_event_type_appears_on_multiple_handlers : Specification
{
    [EventType]
    class SharedEvent;

    class ReactorWithDuplicateEventType : IReactor
    {
        public Task HandleFirst(SharedEvent @event) => Task.CompletedTask;
        public Task HandleSecond(SharedEvent @event) => Task.CompletedTask;
    }

    IEnumerable<Type> _result;

    void Because() => _result = typeof(ReactorWithDuplicateEventType).GetHandlerEventTypes();

    [Fact] void should_return_only_one_entry() => _result.Count().ShouldEqual(1);
    [Fact] void should_contain_the_event_type() => _result.ShouldContain(typeof(SharedEvent));
}
