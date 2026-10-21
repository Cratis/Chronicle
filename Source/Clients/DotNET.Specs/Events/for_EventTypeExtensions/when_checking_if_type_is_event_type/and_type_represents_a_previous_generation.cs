// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_checking_if_type_is_event_type;

public class and_type_represents_a_previous_generation : Specification
{
    bool _result;

    void Because() => _result = typeof(Previous).IsEventType([]);

    [Fact] void should_not_be_considered_an_event_type_without_registration() => _result.ShouldBeFalse();

    [EventTypeGenerationFor<Current>(1)]
    record Previous(string Name);

    [EventType(generation: 2)]
    record Current(string Name);
}
