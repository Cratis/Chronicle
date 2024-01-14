// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Projections.for_ProjectionExtensions.when_applying_from_filter;

public class when_handling_remove : given.an_observable_and_event_setup
{
    void Establish() => observable.RemovedWith(@event.Metadata.Type);

    void Because() => observable.OnNext(event_context);

    [Fact] void should_remove_on_changeset() => changeset.Verify(_ => _.Remove(), Once);
}
