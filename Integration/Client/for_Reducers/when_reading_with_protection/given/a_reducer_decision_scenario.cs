// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Transactions;
using stock = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

namespace Cratis.Chronicle.Integration.for_Reducers.when_reading_with_protection.given;

public class a_reducer_decision_scenario(ChronicleFixture fixture) : Specification(fixture)
{
    public EventSourceId Source = EventSourceId.New();
    public UnitOfWork Unit;
    public DecisionReadCommitOwner Owner;
    public override IEnumerable<Type> EventTypes => [typeof(stock.StockSet), typeof(stock.StockSetV2), typeof(stock.StockRemoved)];
    public override IEnumerable<Type> EventTypeMigrators => [typeof(stock.StockSetToV2Migrator)];
    public override IEnumerable<Type> Reducers => [typeof(StockReducer), typeof(StaticStock)];

    public void CreateUnit()
    {
        Unit = new(CorrelationId.New(), _ => { }, EventStore);
        Owner = Unit.ClaimDecisionReadCommitOwnership();
    }

    public static async Task Independent(Func<Task> action)
    {
        Task task;
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(action);
        }
        await task;
    }

    public override async Task DisposeAsync()
    {
        try
        {
            if (Unit?.IsCompleted == false) await Unit.RollbackAsOwner(Owner);
            Unit?.Dispose();
        }
        finally
        {
            await base.DisposeAsync();
        }
    }
}
