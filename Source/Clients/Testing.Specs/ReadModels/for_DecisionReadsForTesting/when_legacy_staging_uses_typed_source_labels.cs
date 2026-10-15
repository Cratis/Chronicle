// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_legacy_staging_uses_typed_source_labels : given.source_identity
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_share_the_scope_across_legacy_and_ordered_staging(int kind)
    {
        _unit = (UnitOfWork)_store.UnitOfWorkManager.Begin(CorrelationId.New());
        _owner = _unit.ClaimDecisionReadCommitOwnership();
        var source = Source(kind);
        _unit.AddEvent(EventSequenceId.Log, source, new StockChanged(2), Causation.Unknown(), concurrencyScope: new(0, source));
        _unit.AddEvents(EventSequenceId.Log, [new("book", new StockChanged(3))], [new("book", new(0, "book"))]);
        await _unit.CommitAsOwner(_owner);
        Assert.True(_unit.IsSuccess, string.Join(Environment.NewLine, _unit.GetAppendErrors().Select(error => error.Value)));
        _unit.GetEvents().Count().ShouldEqual(2);
    }
}
