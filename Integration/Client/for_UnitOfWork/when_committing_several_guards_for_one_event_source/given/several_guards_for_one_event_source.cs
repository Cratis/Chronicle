// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source.given;

public class several_guards_for_one_event_source(ChronicleFixture fixture) : Specification(fixture)
{
    IDisposable? _withheldConjunctions;
    EventSequenceNumber _approvalsTail;
    EventSequenceNumber _correctionsTail;

    public virtual bool KernelSupportsConjunctions => true;
    public bool AdvertisedConjunctions;
    public EventSourceId Source = EventSourceId.New();
    public UnitOfWork Unit;
    public Exception Error;
    public int Count;

    public override IEnumerable<Type> EventTypes => [typeof(Approved), typeof(Corrected)];

    async Task Establish()
    {
        if (!KernelSupportsConjunctions)
        {
            _withheldConjunctions = ConjunctionSupport.Withhold(EventStore);
        }
        AdvertisedConjunctions = ConjunctionSupport.IsAdvertised(EventStore);
        await EventStore.EventLog.Append(Source, new Approved(), eventStreamType: "approvals", eventStreamId: "sent");
        await EventStore.EventLog.Append(Source, new Corrected(), eventStreamType: "corrections", eventStreamId: "open");
        _approvalsTail = await EventStore.EventLog.GetTailSequenceNumber(Source, eventStreamType: "approvals", eventStreamId: "sent");
        _correctionsTail = await EventStore.EventLog.GetTailSequenceNumber(Source, eventStreamType: "corrections", eventStreamId: "open");
    }

    public void CreateUnit() => Unit = new(CorrelationId.New(), _ => { }, EventStore);

    public void Stage() => Unit.AddEvents(
        EventSequenceId.Log,
        [new(Source, new Approved())],
        [new(Source, new ConcurrencyScope(_approvalsTail, Source, "approvals", "sent")), new(Source, new ConcurrencyScope(_correctionsTail, Source, "corrections", "open"))]);

    public async Task Commit() => Error = await Catch.Exception(Unit.Commit);

    public async Task Independent(Func<Task> append)
    {
        Task competitor;
        using (ExecutionContext.SuppressFlow())
        {
            competitor = Task.Run(append);
        }
        await competitor;
    }

    public async Task<int> StoredCount() => (await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First, Source)).Count;

    public override async Task DisposeAsync()
    {
        try
        {
            if (Unit?.IsCompleted == false)
            {
                await Unit.Rollback();
            }
            Unit?.Dispose();
        }
        finally
        {
            _withheldConjunctions?.Dispose();
            await base.DisposeAsync();
        }
    }
}
