// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Kernel.Projections.for_ProjectionExtensions.when_applying_from_filter;

public class when_handling_remove_with_public_event : given.an_observable_and_event_setup
{
    AppendedEvent public_event;
    ProjectionEventContext public_event_context;

    void Establish()
    {
        observable.RemovedWith(new(@event.Metadata.Type.Id, 1, true));

        public_event = new(
            new(1,
            new(@event.Metadata.Type.Id, 1, true)),
            @event.Context,
            @event.Content);

        public_event_context = new(new(@event.Context.EventSourceId, ArrayIndexers.NoIndexers), @event, changeset.Object);
    }

    void Because() => observable.OnNext(public_event_context);

    [Fact] void should_remove_on_changeset() => changeset.Verify(_ => _.Remove(), Once);
}
