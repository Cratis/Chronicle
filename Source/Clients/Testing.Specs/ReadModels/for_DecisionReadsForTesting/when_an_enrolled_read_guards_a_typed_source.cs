// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_an_enrolled_read_guards_a_typed_source : given.source_identity
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    public async Task should_use_the_guard_without_rewriting_the_staged_source(int kind, bool ordered)
    {
        var read = await _store.GetDecisionReads().GetDetached<BookStock>((ReadModelKey)"book");
        _unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        _owner = _unit.ClaimDecisionReadCommitOwnership();
        _unit.AddDecisionRead(read);
        var source = Source(kind);
        if (ordered) _unit.AddEvents(EventSequenceId.Log, [new(source, new StockChanged(0))], []);
        else _unit.AddEvent(EventSequenceId.Log, source, new StockChanged(0), Causation.Unknown());

        await _unit.CommitAsOwner(_owner);

        Assert.True(_unit.IsSuccess, string.Join(Environment.NewLine, _unit.GetAppendErrors().Select(error => error.Value)));
    }
}
