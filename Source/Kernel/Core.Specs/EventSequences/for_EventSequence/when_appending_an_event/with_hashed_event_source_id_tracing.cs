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

public class with_hashed_event_source_id_tracing : given.an_event_sequence_with_tracing
{
    protected override Telemetry Telemetry => new()
    {
        EventSourceIdTracing = EventSourceIdTracing.Hashed,
        EventSourceIdHashKey = "chronicle-identifier-hash-key-for-specs-0001"
    };

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
    }

    [Fact] void should_hash_the_event_source_id() => _appendActivity.GetTagItem("cratis.eventsource.id").ShouldEqual("8ce3c6dbf2036756ec5ef13d72ec10c6");
    [Fact] void should_preserve_the_stored_event_source_id() => _eventSequenceStorage.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == "Append").GetArguments()[2].ShouldEqual((EventSourceId)"customer-42");
    [Fact] void should_preserve_the_event_source_type() => _appendActivity.GetTagItem("cratis.eventsource.type").ShouldEqual(EventSourceType.Default.Value);
}
