// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class with_default_event_source_id_tracing : given.an_event_sequence_with_tracing
{
    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _eventSequence.Append(EventSourceType.Default, _eventSourceId, EventStreamType.All, EventStreamId.Default, _eventType, new JsonObject(), CorrelationId.New(), [], Identity.System, [], ConcurrencyScope.None);
    }

    [Fact] void should_create_the_append_span() => _appendActivity.ShouldNotBeNull();
    [Fact] void should_expose_the_event_source_id() => _appendActivity.GetTagItem("cratis.eventsource.id").ShouldEqual("customer-42");
    [Fact] void should_preserve_the_stored_event_source_id() => _eventSequenceStorage.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == "Append").GetArguments()[2].ShouldEqual((EventSourceId)"customer-42");
    [Fact] void should_preserve_the_event_source_type() => _appendActivity.GetTagItem("cratis.eventsource.type").ShouldEqual(EventSourceType.Default.Value);
}
