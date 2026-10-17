// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverReaderScope.when_computing_for_an_observer;

public class and_it_has_no_event_types_and_is_not_subscribed_to_all_events : Specification
{
    ObserverReaderScope _result;

    void Because() => _result = ObserverReaderScope.For(false, []);

    [Fact] void should_be_a_wildcard_as_its_readers_have_always_treated_an_empty_list() => _result.IsWildcard.ShouldBeTrue();
    [Fact] void should_hold_no_event_types() => _result.EventTypes.ShouldBeEmpty();
}
