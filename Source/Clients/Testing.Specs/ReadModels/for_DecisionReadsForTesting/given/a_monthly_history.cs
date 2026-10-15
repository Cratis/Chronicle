// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting.given;

public class a_monthly_history : Specification
{
    protected EventStoreForTesting _store;
    protected EventSourceId _source;

    [EventSource("BankAccount")]
    [Cratis.Chronicle.EventSources.EventStream("Transactions")]
    [Cratis.Chronicle.EventSources.EventStream("Limits")]
    public class BankAccount : IEventSource;

    [EventType]
    public record MoneyWithdrawn(decimal Amount);

    [Cratis.Chronicle.ReadModels.EventStream<BankAccount>("Transactions")]
    public record MonthlyWithdrawals(string Id, [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Withdrawn);

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new MoneyWithdrawn(100), "Transactions", "2026-10", "BankAccount");
        await _store.EventLog.Append(_source, new MoneyWithdrawn(200), "Transactions", "2026-11", "BankAccount");
        await _store.EventLog.Append(_source, new MoneyWithdrawn(300), "Limits", "2026-10", "BankAccount");
        await _store.EventLog.Append(EventSourceId.New(), new MoneyWithdrawn(400), "Transactions", "2026-10", "BankAccount");
        await _store.EventLog.Append(_source, new MoneyWithdrawn(500), "Transactions", "2026-10");
    }

    protected Task<DecisionRead<MonthlyWithdrawals>> ReadMonth() =>
        _store.GetDecisionReads().GetDetached<MonthlyWithdrawals>(_source, "Transactions", "2026-10", "BankAccount");

    protected async Task<IUnitOfWork> Commit(DecisionRead<MonthlyWithdrawals> read)
    {
        var unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        unit.AddDecisionRead(read);
        await unit.CommitAsOwner(unit.ClaimDecisionReadCommitOwnership());
        return unit;
    }

    void Destroy() => _store.Dispose();
}
