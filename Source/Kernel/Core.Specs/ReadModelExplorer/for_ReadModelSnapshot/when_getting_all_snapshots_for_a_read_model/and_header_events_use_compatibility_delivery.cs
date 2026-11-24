// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.ReadModelExplorer.for_ReadModelSnapshot.when_getting_all_snapshots_for_a_read_model;

public class and_header_events_use_compatibility_delivery : given.a_header_event_history
{
    async Task Because() => _result = await AllSnapshots();

    [Fact] void should_redact_all_content_variants_before_reconstruction() => header_events.ContainsSentinel(JsonSerializer.Serialize(_projected)).ShouldBeFalse();
    [Fact] void should_redact_display_and_reconstructed_state() => header_events.ContainsSentinel(JsonSerializer.Serialize(_result)).ShouldBeFalse();
    [Fact] void should_preserve_correlation_grouping() => _result.Count().ShouldEqual(1);
    [Fact] void should_keep_every_event() => _projected.Count.ShouldEqual(_stored.Length);
    [Fact] void should_preserve_internal_values() => _stored.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
