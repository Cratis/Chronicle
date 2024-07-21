// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactions.for_ObserverInvoker.when_creating_for;

public class reaction_with_handler_method_for_interface_event_type_with_derivatives : given.an_reaction_invoker_for<ReactionWithHandlerMethodForInterfaceEventTypeWithDerivatives>
{
    protected override IEnumerable<Type> GetEventTypes() => [typeof(MyFirstDerivedEventFromInterface), typeof(MySecondDerivedEventFromInterface)];

    [Fact] void should_have_the_event_type() => invoker.EventTypes.ShouldContainOnly(MyFirstDerivedEventFromInterface.EventType, MySecondDerivedEventFromInterface.EventType);
}
