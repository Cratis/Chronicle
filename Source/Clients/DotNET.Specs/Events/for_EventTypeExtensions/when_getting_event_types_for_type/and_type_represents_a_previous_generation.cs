// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_represents_a_previous_generation : Specification
{
    IEnumerable<Type> _result;

    void Because() => _result = typeof(Previous).GetEventTypes([typeof(Previous), typeof(Current)]);

    [Fact] void should_not_expand_to_the_previous_generation_type() => _result.ShouldBeEmpty();

    [EventTypeGenerationFor<Current>(1)]
    record Previous(string Name);

    [EventType(generation: 2)]
    record Current(string Name);
}
