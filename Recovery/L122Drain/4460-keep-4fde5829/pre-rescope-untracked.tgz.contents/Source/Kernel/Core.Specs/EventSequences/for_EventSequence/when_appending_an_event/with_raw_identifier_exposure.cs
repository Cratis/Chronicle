// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class with_raw_identifier_exposure : given.an_event_sequence_with_tracing
{
    protected override Telemetry Telemetry => new() { EventSourceIdsInSpans = IdentifierExposure.Raw };

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
    }

    [Fact] void should_expose_the_event_source_id() => _appendActivity.GetTagItem("cratis.eventsource.id").ShouldEqual("customer-42");
    [Fact] void should_preserve_the_event_source_type() => _appendActivity.GetTagItem("cratis.eventsource.type").ShouldEqual(EventSourceType.Default.Value);
}
