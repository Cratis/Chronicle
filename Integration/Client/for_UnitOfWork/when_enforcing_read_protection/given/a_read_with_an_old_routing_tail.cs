// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

public class a_read_with_an_old_routing_tail(ChronicleFixture fixture) : a_read_protection_scenario(fixture)
{
    public int Count;
    public EventSequenceNumber RoutingTail;

    async Task Establish()
    {
        await EventStore.EventLog.Append(Source, new RoutingFact(), eventStreamType: "numbering");
        await EventStore.EventLog.Append(Source, new StockSet(1));
        await EventStore.EventLog.AppendMany(Enumerable.Range(0, 99).Select(_ => new EventForEventSourceId("other-source", new RoutingFact())));
    }

    public async Task CaptureRoutingTail()
    {
        using var reads = Unit.AllowReads(ReadAllowance.Contributed, "observe the historical declared routing tail");
        RoutingTail = await EventStore.EventLog.GetTailSequenceNumber(Source, eventStreamType: "numbering");
    }

    public void StageDeclared() => Stage(options: new EventEnrollmentOptions
    {
        EventStreamType = "numbering",
        DeclaredConcurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType
    });
}
