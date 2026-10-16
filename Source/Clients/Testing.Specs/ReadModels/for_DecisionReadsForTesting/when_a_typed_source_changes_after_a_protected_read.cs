// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_typed_source_changes_after_a_protected_read : given.source_identity
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_preserve_the_read_boundary_and_refuse_the_commit(int kind)
    {
        using var frame = Begin();
        await _store.GetDecisionReads().GetFor<BookStock>(_unit, (ReadModelKey)"book");
        Task competitor;
        using (ExecutionContext.SuppressFlow())
        {
            competitor = Task.Run(async () => (await _store.EventLog.Append(Source(kind), new StockChanged(0))).IsSuccess.ShouldBeTrue());
        }
        await competitor;
        Stage(Source(kind));
        await _unit.CommitAsOwner(_owner);
        _unit.IsSuccess.ShouldBeFalse();
        _unit.GetConcurrencyViolations().ShouldNotBeEmpty();
        _unit.GetAppendConditions().Count.ShouldEqual(1);
        _unit.GetDecisionConflicts().Single().Key.ShouldEqual((ReadModelKey)"book");
    }
}
