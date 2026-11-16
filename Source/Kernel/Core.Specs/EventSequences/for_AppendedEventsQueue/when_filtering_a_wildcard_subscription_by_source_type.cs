// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue;

public class when_filtering_a_wildcard_subscription_by_source_type : given.subscription_matching
{
    void Establish()
    {
        _subscription = _subscription with { AllEventTypes = true, Filters = new ObserverFilters([], new EventSourceType("order")) };
        _events[1] = _events[1] with { Context = _events[1].Context with { EventSourceType = new EventSourceType("order") } };
    }

    void Because() => Filter();

    [Fact] void should_still_apply_source_type_filters() => _result.ShouldEqual(_events[1]);
}
