// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ObserverInvoker.when_creating_for;

public class reactor_with_handler_method_for_interface_event_type_with_derivatives : given.an_reactor_invoker_for<ReactorWithHandlerMethodForInterfaceEventTypeWithDerivatives>
{
    protected override IEnumerable<Type> GetEventTypes() => [typeof(MyFirstDerivedEventFromInterface), typeof(MySecondDerivedEventFromInterface)];

    [Fact] void should_have_the_event_type() => _reactorEventTypes.ShouldContainOnly(MyFirstDerivedEventFromInterface.EventType, MySecondDerivedEventFromInterface.EventType);
}
