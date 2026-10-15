// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting.given;

public class source_identity : Specification
{
    protected EventStoreForTesting _store;
    protected UnitOfWork _unit;
    protected DecisionReadCommitOwner _owner;

    async Task Establish()
    {
        _store = new EventStoreForTesting(null, new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(StockChanged)],
            ModelBoundProjections = [typeof(BookStock)]
        });
        (await _store.EventLog.Append("book", new StockChanged(1))).IsSuccess.ShouldBeTrue();
    }

    protected static EventSourceId Source(int kind) => kind switch
    {
        1 => new BookId("book"),
        2 => new EventSourceId<string>("book"),
        3 => new BookConcept("book"),
        _ => new EventSourceId("book")
    };

    protected ReadProtectionFrame Begin()
    {
        _unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        _owner = _unit.ClaimDecisionReadCommitOwnership();
        return _unit.EnforceReadProtection();
    }

    protected void Stage(EventSourceId source) => _unit.AddEventWithOptions(EventSequenceId.Log, source, new StockChanged(0), Causation.Unknown(), new()
    {
        Origin = ConcurrencyScopeOrigin.Routing
    });

    async Task Destroy()
    {
        if (_unit?.IsCompleted == false) await _unit.RollbackAsOwner(_owner);
        _store.Dispose();
    }

    public record BookId(string Value) : EventSourceId(Value);

    [EventType]
    public record StockChanged(int Available);

    [Passive, FromEvent<StockChanged>]
    public record BookStock([property: Key] BookId Id, int Available);

    public record BookConcept(string Value) : ConceptAs<string>(Value)
    {
        public static implicit operator EventSourceId(BookConcept value) => new BookId(value.Value);
    }
}
