// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerTypeExtensions.when_getting_handler_event_types;

public class and_same_event_type_appears_on_multiple_handlers : Specification
{
    [EventType]
    class SharedEvent;

    record MyReadModel(string Value);

    class ReducerWithDuplicateEventType : IReducerFor<MyReadModel>
    {
        public MyReadModel? ReduceFirst(SharedEvent @event, MyReadModel? current) => current;
        public MyReadModel? ReduceSecond(SharedEvent @event, MyReadModel? current) => current;
    }

    IEnumerable<Type> _result;

    void Because() => _result = typeof(ReducerWithDuplicateEventType).GetHandlerEventTypes();

    [Fact] void should_return_only_one_entry() => _result.Count().ShouldEqual(1);
    [Fact] void should_contain_the_event_type() => _result.ShouldContain(typeof(SharedEvent));
}
