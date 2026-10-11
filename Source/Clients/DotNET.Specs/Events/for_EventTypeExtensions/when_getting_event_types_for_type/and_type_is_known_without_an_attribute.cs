// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_is_known_without_an_attribute : Specification
{
    Type[] _result;

    void Because() => _result = typeof(Known).GetEventTypes([typeof(Known), typeof(Derived), typeof(Known)]).ToArray();

    [Fact] void should_include_the_exact_type_once() => _result.Count(type => type == typeof(Known)).ShouldEqual(1);
    [Fact] void should_include_the_derived_type() => _result.ShouldContain(typeof(Derived));

    class Known;
    class Derived : Known;
}
