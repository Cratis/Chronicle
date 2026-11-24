// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Sequences.for_ExportedEvent;

public class when_exporting_header_events : Sequences.given.a_header_event_query
{
    IEnumerable<ExportedEvent> _result;

    async Task Because() => _result = await ExportedEvent.ExportEvents(_storage, _compliance, header_events.Options, _queryContext, "application-store", "namespace", "system");

    [Fact] void should_redact_exported_content() => header_events.ContainsSentinel(JsonSerializer.Serialize(_result)).ShouldBeFalse();
    [Fact] void should_keep_events() => _result.Count().ShouldEqual(_stored.Length);
    [Fact] void should_keep_header_names() => _result.All(@event => header_events.HasEmptyHeaders(@event.Content)).ShouldBeTrue();
    [Fact] void should_preserve_internal_values() => _stored.All(@event => header_events.ContainsSentinel(JsonSerializer.Serialize(@event))).ShouldBeTrue();
}
