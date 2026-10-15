// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsGauge.when_observing;

public class and_a_previous_owner_clears_after_a_new_owner_published : given.a_gauge
{
    readonly object _previous = new();

    void Establish()
    {
        _holder.Activate(_previous);
        _holder.Activate(this);
        _holder.Publish(this, Snapshot(2));
    }

    void Because() => _holder.Clear(_previous);

    [Fact] void should_keep_the_new_owners_data() => _gauge.Observe().Single().Value.ShouldEqual(2L);
}
