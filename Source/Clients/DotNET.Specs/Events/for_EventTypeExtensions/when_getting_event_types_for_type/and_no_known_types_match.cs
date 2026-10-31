// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventTypeExtensions.when_getting_event_types_for_type;

public class and_no_known_types_match : Specification
{
    IEnumerable<Type> _empty;
    IEnumerable<Type> _nonmatching;

    void Because()
    {
        _empty = typeof(Known).GetEventTypes([]);
        _nonmatching = typeof(Known).GetEventTypes([typeof(string)]);
    }

    [Fact] void should_not_expand_an_empty_registry() => _empty.ShouldBeEmpty();
    [Fact] void should_not_expand_nonmatching_types() => _nonmatching.ShouldBeEmpty();

    class Known;
}
