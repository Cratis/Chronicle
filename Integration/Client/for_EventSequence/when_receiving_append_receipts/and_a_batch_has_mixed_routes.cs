// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts.and_a_batch_has_mixed_routes.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts;

[Collection(ChronicleCollection.Name)]
public class and_a_batch_has_mixed_routes(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public Contracts.Sequences.AppendManyResponse Response;
        public Contracts.Sequences.EventContext[] Receipts;
        public IImmutableList<AppendedEvent> Stored;
        public override IEnumerable<Type> EventTypes => [typeof(ReceiptEvent)];

        async Task Because()
        {
            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var eventType = EventStore.EventTypes.GetEventTypeFor(typeof(ReceiptEvent));
            var contractType = new Contracts.Sequences.EventType { Id = eventType.Id, Generation = eventType.Generation.Value };
            Response = await services.Sequences.AppendManyForEventSources(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                EventSequenceId = EventStore.EventLog.Id,
                IncludeReceipts = true,
                Causation = [new() { Type = "batch-cause", Occurred = new DateTimeOffset(2019, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(7654321) }],
                Events =
                [
                    new() { EventSourceId = "receipt-first", EventType = contractType, Content = JsonSerializer.Serialize(new ReceiptEvent("first")), Occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(1234567) },
                    new() { EventSourceId = "receipt-second", EventSourceType = "Account", EventStreamType = "Payments", EventStreamId = "period", EventType = contractType, Content = JsonSerializer.Serialize(new ReceiptEvent("second")), Tags = ["second-tag"], Subject = "second-subject", Occurred = new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero).AddTicks(8765432) }
                ]
            }).EnsureSuccess();
            Receipts = Response.Receipts.ToArray();
            Stored = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        }
    }

    [Fact] void should_succeed() => Context.Response.IsSuccess.ShouldBeTrue();
    [Fact] void should_return_one_receipt_per_input() => Context.Receipts.Length.ShouldEqual(2);
    [Fact] void should_preserve_input_order() => Context.Receipts.Select(_ => _.EventSourceId).ShouldEqual(["receipt-first", "receipt-second"]);
    [Fact] void should_pair_receipts_with_acknowledged_numbers() => Context.Receipts.Select(_ => _.SequenceNumber).ShouldEqual(Context.Response.SequenceNumbers);
    [Fact] void should_report_kernel_defaults() => Context.Receipts[0].EventStreamType.ShouldEqual(EventStreamType.All.Value);
    [Fact] void should_preserve_the_explicit_route() => Context.Receipts[1].EventStreamId.ShouldEqual("period");
    [Fact] void should_report_persisted_routes() => Context.Receipts.Select(_ => _.EventStreamType).ShouldEqual(Context.Stored.Select(_ => _.Context.EventStreamType.Value));
    [Fact] void should_report_persisted_subjects() => Context.Receipts.Select(_ => _.Subject).ShouldEqual(Context.Stored.Select(_ => _.Context.Subject.Value));
    [Fact] void should_report_persisted_hashes() => Context.Receipts.Select(_ => _.Hash).ShouldEqual(Context.Stored.Select(_ => _.Context.Hash.Value));
    [Fact] void should_report_persisted_occurrence_times() => Context.Receipts.Select(_ => DateTimeOffset.Parse(_.Occurred.Value, CultureInfo.InvariantCulture)).ShouldEqual(Context.Stored.Select(_ => _.Context.Occurred));
    [Fact] void should_report_persisted_causation_times() => Context.Receipts.SelectMany(_ => _.Causation).Select(_ => DateTimeOffset.Parse(_.Occurred.Value, CultureInfo.InvariantCulture)).ShouldEqual(Context.Stored.SelectMany(_ => _.Context.Causation).Select(_ => _.Occurred));
}
