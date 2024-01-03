// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.for_ProjectionExtensions.when_applying_from_filter;

public class and_issuing_event_with_type_that_is_not_included : given.an_observable_and_event_setup
{
    IObservable<ProjectionEventContext> filtered;

    void Establish()
    {
        filtered = observable.WhereEventTypeEquals(new EventType("745a8adb-aec5-4bf7-af29-b23d14e5c7bc", 1));
        filtered.Subscribe(received.Add);
    }

    void Because() => observable.OnNext(event_context);

    [Fact] void should_not_get_any_events() => received.ShouldBeEmpty();
}
