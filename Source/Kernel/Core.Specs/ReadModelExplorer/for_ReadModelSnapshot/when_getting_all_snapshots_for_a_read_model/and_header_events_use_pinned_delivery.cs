// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_header_events_use_pinned_delivery : given.a_header_event_history
{
    protected override EventGenerationDelivery Delivery => EventGenerationDelivery.Pinned;

    async Task Because() => _result = await AllSnapshots(nameof(ReadModelSnapshotGrouping.Event));

    [Fact] void should_redact_current_and_historical_inputs_before_generation_selection() => header_events.ContainsSentinel(JsonSerializer.Serialize(_projected)).ShouldBeFalse();
    [Fact] void should_redact_display_after_compliance_release() => header_events.ContainsSentinel(JsonSerializer.Serialize(_result)).ShouldBeFalse();
    [Fact] void should_preserve_event_grouping() => _result.Count().ShouldEqual(_stored.Length);
    [Fact] void should_keep_every_event() => _projected.Count.ShouldEqual(_stored.Length);
    [Fact] void should_preserve_internal_values() => _stored.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
