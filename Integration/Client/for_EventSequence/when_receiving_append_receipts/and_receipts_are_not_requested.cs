// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts.and_receipts_are_not_requested.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts;

[Collection(ChronicleCollection.Name)]
public class and_receipts_are_not_requested(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public Contracts.Sequences.AppendResponse Response;
        public Contracts.Sequences.AppendManyResponse Batch;
        public override IEnumerable<Type> EventTypes => [typeof(ReceiptEvent)];

        async Task Because()
        {
            var services = ((IChronicleServicesAccessor)EventStore.Connection).Services;
            var eventType = EventStore.EventTypes.GetEventTypeFor(typeof(ReceiptEvent));
            var contractType = new Contracts.Sequences.EventType { Id = eventType.Id, Generation = eventType.Generation.Value };
            var content = JsonSerializer.Serialize(new ReceiptEvent("value"));
            Response = await services.Sequences.Append(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                EventSequenceId = EventStore.EventLog.Id,
                EventSourceId = "legacy-single",
                EventType = contractType,
                Content = content
            }).EnsureSuccess();
            Batch = await services.Sequences.AppendMany(new()
            {
                EventStore = EventStore.Name,
                Namespace = EventStore.Namespace,
                EventSequenceId = EventStore.EventLog.Id,
                EventSourceId = "legacy-batch",
                Events = [new() { EventType = contractType, Content = content }]
            }).EnsureSuccess();
        }
    }

    [Fact] void should_accept_the_single_append() => Context.Response.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_single_acknowledgment_compact() => Context.Response.Receipt.ShouldBeNull();
    [Fact] void should_accept_the_batch() => Context.Batch.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_batch_acknowledgment_compact() => Context.Batch.Receipts.ShouldBeEmpty();
}
