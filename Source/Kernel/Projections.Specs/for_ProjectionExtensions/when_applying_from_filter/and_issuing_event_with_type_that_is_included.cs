// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionExtensions.when_applying_from_filter;

public class and_issuing_event_with_type_that_is_included : given.an_observable_and_event_setup
{
    IObservable<ProjectionEventContext> filtered;

    void Establish()
    {
        filtered = _observable.WhereEventTypeEquals(_eventContext.Event.Metadata.Type);
        filtered.Subscribe(_received.Add);
    }

    void Because() => _observable.OnNext(_eventContext);

    [Fact] void should_receive_event() => _received.ShouldContainOnly(_eventContext);
}
