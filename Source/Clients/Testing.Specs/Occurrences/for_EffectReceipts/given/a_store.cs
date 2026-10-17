// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Occurrences;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.Occurrences.for_EffectReceipts.given;

public class a_store : Specification
{
    protected EventStoreForTesting _store;
    protected EffectKey _key;

    void Establish()
    {
        _store = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(SomethingHappened)], Triggers = [typeof(WorkRequested)] });
        _key = new OccurrenceContextBuilder().ForTrigger(new("testing-receipt")).WithIdempotencyId("one").BuildDetached().EffectKey("write");
    }

    protected UnitOfWork UnitWithEvent(string value)
    {
        var unit = new UnitOfWork(CorrelationId.New(), _ => { }, _store);
        unit.UseEffectReceipt(_key);
        unit.AddEvent(EventSequenceId.Log, "source", new SomethingHappened(value), Causation.Unknown());
        return unit;
    }

    void Destroy() => _store.Dispose();

    [EventType]
    public record SomethingHappened(string Value);

    [Trigger("receipt-work")]
    public record WorkRequested;
}
