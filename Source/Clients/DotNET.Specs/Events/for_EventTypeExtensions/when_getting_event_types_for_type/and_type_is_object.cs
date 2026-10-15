// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_type_is_object : Specification
{
    IEnumerable<Type> _result;

    void Because() => _result = typeof(object).GetEventTypes([typeof(object), typeof(string)]);

    [Fact] void should_not_expand_object_to_every_registered_type() => _result.ShouldBeEmpty();
}
