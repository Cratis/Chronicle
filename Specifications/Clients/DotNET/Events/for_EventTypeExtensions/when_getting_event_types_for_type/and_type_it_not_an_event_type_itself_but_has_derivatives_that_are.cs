// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_it_not_an_event_type_itself_but_has_derivatives_that_are : Specification
{
    class MyEvent
    {
    }

    class FirstDerivative : MyEvent
    {
    }

    class SecondDerivative : FirstDerivative
    {
    }

    IEnumerable<Type> result;

    void Because() => result = typeof(MyEvent).GetEventTypes(new[] { typeof(FirstDerivative), typeof(SecondDerivative) });

    [Fact] void should_return_the_expected_types() => result.ShouldContainOnly(typeof(FirstDerivative), typeof(SecondDerivative));
}
