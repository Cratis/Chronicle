// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_getting_handler_event_types;

public class and_reducer_has_handler_methods_with_event_types : Specification
{
    [EventType]
    class FirstEvent;

    [EventType]
    class SecondEvent;

    record MyReadModel(string Value);

    class ReducerWithTwoEventTypes : IReducerFor<MyReadModel>
    {
        public MyReadModel? Reduce(FirstEvent @event, MyReadModel? current) => current;
        public MyReadModel? Reduce(SecondEvent @event, MyReadModel? current) => current;
    }

    IEnumerable<Type> _result;

    void Because() => _result = typeof(ReducerWithTwoEventTypes).GetHandlerEventTypes();

    [Fact] void should_return_all_event_types() => _result.Count().ShouldEqual(2);
    [Fact] void should_include_first_event_type() => _result.ShouldContain(typeof(FirstEvent));
    [Fact] void should_include_second_event_type() => _result.ShouldContain(typeof(SecondEvent));
}
