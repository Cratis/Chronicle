// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_previewing;

public class and_header_events_are_read_with_an_inferred_model : given.a_header_event_preview
{
    void Establish() => SetCompiledDefinition(_projectionDefinition with { ReadModel = ReadModelIdentifier.Inferred });

    async Task Because() => _entries = (await _service.Preview(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "system",
        Declaration = "projection PreviewHeaders"
    })).Value0.ReadModelEntries.ToArray();

    [Fact] void should_redact_before_processing() => header_events.ContainsSentinel(JsonSerializer.Serialize(_previewed)).ShouldBeFalse();
    [Fact] void should_not_copy_header_values_into_returned_state() => _entries.All(entry => !header_events.ContainsSentinel(entry)).ShouldBeTrue();
    [Fact] void should_keep_every_event() => _previewed.Length.ShouldEqual(_stored.Length);
    [Fact] void should_preserve_internal_content() => _stored.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
