// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_computing_for_an_observer;

public class and_it_has_event_types : Specification
{
    static readonly EventType _first = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    static readonly EventType _second = new("22222222-2222-2222-2222-222222222222", EventTypeGeneration.First);
    ObserverReaderScope _result;

    void Because() => _result = ObserverReaderScope.For(false, [_first, _second]);

    [Fact] void should_not_be_a_wildcard() => _result.IsWildcard.ShouldBeFalse();
    [Fact] void should_hold_the_event_types() => _result.EventTypes.ShouldContainOnly([_first, _second]);
}
