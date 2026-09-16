// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts.and_a_single_event_is_stored.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts;

[Collection(ChronicleCollection.Name)]
public class and_a_single_event_is_stored(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public Contracts.Sequences.AppendResponse Response;
        public AppendedEvent Stored;
        public override IEnumerable<Type> EventTypes => [typeof(ReceiptEvent)];

        async Task Because()
        {
            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var eventType = EventStore.EventTypes.GetEventTypeFor(typeof(ReceiptEvent));
            Response = await services.Sequences.Append(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                EventSequenceId = EventStore.EventLog.Id,
                EventSourceId = "receipt-source",
                IncludeReceipt = true,
                EventType = new() { Id = eventType.Id, Generation = eventType.Generation.Value },
                Content = JsonSerializer.Serialize(new ReceiptEvent("value")),
                Subject = "receipt-subject",
                Tags = ["receipt-tag"],
                Occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567),
                Causation = [new() { Type = "receipt-cause", Occurred = new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(7654321), Properties = new Dictionary<string, string> { ["key"] = "value" } }],
                CausedBy = new() { Subject = "caller", Name = "Caller", UserName = "caller" }
            }).EnsureSuccess();
            Stored = (await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, "receipt-source")).Single();
        }
    }

    [Fact] void should_succeed() => Context.Response.IsSuccess.ShouldBeTrue();
    [Fact] void should_include_a_receipt() => Context.Response.Receipt.ShouldNotBeNull();
    [Fact] void should_report_the_persisted_sequence_number() => Context.Response.Receipt!.SequenceNumber.ShouldEqual(Context.Stored.Context.SequenceNumber.Value);
    [Fact] void should_report_the_persisted_store() => Context.Response.Receipt!.EventStore.ShouldEqual(Context.Stored.Context.EventStore.Value);
    [Fact] void should_report_the_persisted_namespace() => Context.Response.Receipt!.Namespace.ShouldEqual(Context.Stored.Context.Namespace.Value);
    [Fact] void should_report_the_persisted_source_type() => Context.Response.Receipt!.EventSourceType.ShouldEqual(Context.Stored.Context.EventSourceType.Value);
    [Fact] void should_report_the_persisted_stream_type() => Context.Response.Receipt!.EventStreamType.ShouldEqual(Context.Stored.Context.EventStreamType.Value);
    [Fact] void should_report_the_persisted_stream_id() => Context.Response.Receipt!.EventStreamId.ShouldEqual(Context.Stored.Context.EventStreamId.Value);
    [Fact] void should_report_the_persisted_occurrence() => DateTimeOffset.Parse(Context.Response.Receipt!.Occurred.Value, CultureInfo.InvariantCulture).ShouldEqual(Context.Stored.Context.Occurred);
    [Fact] void should_report_the_persisted_subject() => Context.Response.Receipt!.Subject.ShouldEqual(Context.Stored.Context.Subject.Value);
    [Fact] void should_report_the_persisted_tags() => Context.Response.Receipt!.Tags.ShouldEqual(Context.Stored.Context.Tags.Select(_ => _.Value));
    [Fact] void should_report_the_persisted_hash() => Context.Response.Receipt!.Hash.ShouldEqual(Context.Stored.Context.Hash.Value);
    [Fact] void should_report_the_persisted_causation() => Context.Response.Receipt!.Causation.Select(_ => _.Type).ShouldEqual(Context.Stored.Context.Causation.Select(_ => _.Type.Value));
    [Fact] void should_report_the_persisted_identity() => Context.Response.Receipt!.CausedBy.Subject.ShouldEqual(Context.Stored.Context.CausedBy.Subject);
    [Fact] void should_report_the_persisted_causation_timestamps() => Context.Response.Receipt!.Causation.Select(_ => DateTimeOffset.Parse(_.Occurred.Value, CultureInfo.InvariantCulture)).ShouldEqual(Context.Stored.Context.Causation.Select(_ => _.Occurred));
}
