// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.for_ProjectionExtensions.when_applying_from_filter;

public class and_issuing_public_event_with_type_that_is_included : given.an_observable_and_event_setup
{
    IObservable<ProjectionEventContext> filtered;

    void Establish()
    {
        filtered = observable.WhereEventTypeEquals(new EventType(@event.Metadata.Type.Id, 1, true));
        filtered.Subscribe(received.Add);
    }

    void Because() => observable.OnNext(event_context);

    [Fact] void should_receive_event() => received.ShouldContainOnly(event_context);
}
