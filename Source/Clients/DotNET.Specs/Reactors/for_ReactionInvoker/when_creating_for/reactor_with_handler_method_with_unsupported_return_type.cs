// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class reactor_with_handler_method_with_unsupported_return_type : Specification
{
    IEventTypes _eventTypes;
    Exception _result;

    void Establish() => _eventTypes = new EventTypesForSpecifications([typeof(MyEvent)]);

    void Because() => _result = Catch.Exception(() => ReactorInvoker.GetEventTypesFor(_eventTypes, typeof(ReactorWithHandlerMethodWithUnsupportedReturnType)));

    [Fact] void should_fail_because_of_the_unsupported_return_type() => _result.ShouldBeOfExactType<InvalidReactorHandlerReturnType>();
}
