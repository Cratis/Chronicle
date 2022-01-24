// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.for_ProjectionExtensions.when_applying_from_filter
{
    public class and_issuing_event_with_type_that_is_included : given.an_observable_and_event_setup
    {
        IObservable<ProjectionEventContext> filtered;

        void Establish()
        {
            filtered = observable.From(event_context.Event.Metadata.Type);
            filtered.Subscribe(_ => received.Add(_));
        }

        void Because() => observable.OnNext(event_context);

        [Fact] void should_receive_event() => received.ShouldContainOnly(event_context);
    }
}
