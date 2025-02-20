// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_ProjectionExtensions.when_applying_from_filter;

public class and_issuing_event_with_type_that_is_not_included : given.an_observable_and_event_setup
{
    IObservable<ProjectionEventContext> filtered;

    void Establish()
    {
        filtered = _observable.WhereEventTypeEquals(new EventType("745a8adb-aec5-4bf7-af29-b23d14e5c7bc", 1));
        filtered.Subscribe(_received.Add);
    }

    void Because() => _observable.OnNext(_eventContext);

    [Fact] void should_not_get_any_events() => _received.ShouldBeEmpty();
}
