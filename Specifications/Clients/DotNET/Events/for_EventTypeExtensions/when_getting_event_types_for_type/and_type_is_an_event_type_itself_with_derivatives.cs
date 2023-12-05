// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_is_an_event_type_itself_with_derivatives : Specification
{
    [EventType("01547ac7-25f6-439b-bc20-2422479ef29a")]
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

    void Because() => result = typeof(MyEvent).GetEventTypes(new[] { typeof(MyEvent), typeof(FirstDerivative), typeof(SecondDerivative) });

    [Fact] void should_return_the_expected_types() => result.ShouldContainOnly(typeof(MyEvent), typeof(FirstDerivative), typeof(SecondDerivative));
}
