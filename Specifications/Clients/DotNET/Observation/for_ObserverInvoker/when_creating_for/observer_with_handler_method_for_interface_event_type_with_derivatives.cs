// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Observation.for_ObserverInvoker.when_creating_for;

public class observer_with_handler_method_for_interface_event_type_with_derivatives : given.an_observer_invoker_for<ObserverWithHandlerMethodForInterfaceEventTypeWithDerivatives>
{
    protected override IEnumerable<Type> GetEventTypes() => new[] { typeof(MyFirstDerivedEventFromInterface), typeof(MySecondDerivedEventFromInterface) };

    [Fact] void should_have_the_event_type() => invoker.EventTypes.ShouldContainOnly(MyFirstDerivedEventFromInterface.EventType, MySecondDerivedEventFromInterface.EventType);
}
