// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_it_not_an_event_type_itself_but_has_derivatives_that_are : Specification
{
    class MyEvent;

    class FirstDerivative : MyEvent;

    class SecondDerivative : FirstDerivative;

    IEnumerable<Type> _result;

    void Because() => _result = typeof(MyEvent).GetEventTypes([typeof(FirstDerivative), typeof(SecondDerivative)]);

    [Fact] void should_return_the_expected_types() => _result.ShouldContainOnly(typeof(FirstDerivative), typeof(SecondDerivative));
}
