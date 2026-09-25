// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.and_wire_metadata_is_omitted.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class and_wire_metadata_is_omitted(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public IImmutableList<AppendedEvent> Stored;
        public Contracts.Sequences.AppendResponse Result;
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Because()
        {
            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var eventType = EventStore.EventTypes.GetEventTypeFor(typeof(SomeEvent));
            Result = await services.Sequences.Append(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                EventSequenceId = EventStore.EventLog.Id,
                EventSourceId = "wire-metadata-source",
                EventType = new() { Id = eventType.Id, Generation = eventType.Generation.Value, Tombstone = eventType.Tombstone },
                Content = JsonSerializer.Serialize(new SomeEvent("some content"))
            }).EnsureSuccess();
            Stored = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, "wire-metadata-source");
        }
    }

    [Fact] void should_append_successfully() => Context.Result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_no_constraint_violations() => Context.Result.ConstraintViolations.ShouldBeEmpty();
    [Fact] void should_store_one_event() => Context.Stored.Count.ShouldEqual(1);
    [Fact] void should_store_default_source_type() => Context.Stored[0].Context.EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_store_all_stream_type() => Context.Stored[0].Context.EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_store_default_stream_id() => Context.Stored[0].Context.EventStreamId.Value.ShouldEqual(EventStreamId.Default);
}
