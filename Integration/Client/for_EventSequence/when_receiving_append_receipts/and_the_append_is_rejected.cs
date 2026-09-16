// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;

using context = Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts.and_the_append_is_rejected.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_receiving_append_receipts;

[Collection(ChronicleCollection.Name)]
public class and_the_append_is_rejected(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public Contracts.Sequences.AppendResponse Response;
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
                EventSourceId = "rejected-source",
                IncludeReceipt = true,
                EventType = new() { Id = eventType.Id, Generation = eventType.Generation.Value },
                Content = "{}"
            }).EnsureSuccess();
        }
    }

    [Fact] void should_reject_the_append() => Context.Response.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_constraint_violation() => Context.Response.HasConstraintViolations.ShouldBeTrue();
    [Fact] void should_not_claim_a_persisted_receipt() => Context.Response.Receipt.ShouldBeNull();
    [Fact] Task should_not_store_an_event() => Context.ShouldHaveNextSequenceNumber(EventSequenceNumber.First);
}
