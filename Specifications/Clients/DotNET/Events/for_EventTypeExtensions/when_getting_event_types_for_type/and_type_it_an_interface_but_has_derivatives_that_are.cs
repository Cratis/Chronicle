// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_it_an_interface_but_has_derivatives_that_are : Specification
{
    interface IMyEvent;

    class FirstDerivative : IMyEvent;

    class SecondDerivative : FirstDerivative;

    IEnumerable<Type> result;

    void Because() => result = typeof(IMyEvent).GetEventTypes([typeof(FirstDerivative), typeof(SecondDerivative)]);

    [Fact] void should_return_the_expected_types() => result.ShouldContainOnly(typeof(FirstDerivative), typeof(SecondDerivative));
}
