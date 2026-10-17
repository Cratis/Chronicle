// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_computing_for_an_observer;

public class and_it_is_subscribed_to_all_events : Specification
{
    static readonly EventType _first = new("11111111-1111-1111-1111-111111111111", EventTypeGeneration.First);
    ObserverReaderScope _result;

    void Because() => _result = ObserverReaderScope.For(true, [_first]);

    [Fact] void should_be_a_wildcard() => _result.IsWildcard.ShouldBeTrue();
    [Fact] void should_hold_no_event_types() => _result.EventTypes.ShouldBeEmpty();
}
