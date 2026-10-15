// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_typed_source_contributions_are_combined : given.source_identity
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_merge_equal_wire_labels_and_predicates_into_one_condition(int kind)
    {
        using var frame = Begin();
        var source = Source(kind);
        _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Explicit, source, new(0, source), _store.Name, _store.Namespace, EventSequenceId.Log, "typed read"));
        _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Explicit, "book", new(0, "book"), _store.Name, _store.Namespace, EventSequenceId.Log, "base read"));
        await _store.GetDecisionReads().GetFor<BookStock>(_unit, (ReadModelKey)"book");
        Stage(source);
        await _unit.CommitAsOwner(_owner);
        Assert.True(_unit.IsSuccess, string.Join(Environment.NewLine, _unit.GetAppendErrors().Select(error => error.Value)));
        var condition = _unit.GetAppendConditions().Single();
        condition.Label.ShouldEqual("book");
        condition.EventSourceId!.Value.ShouldEqual("book");
        condition.Origins.ShouldContain(AppendConditionOrigin.Explicit);
        condition.Origins.ShouldContain(AppendConditionOrigin.ProtectedRead);
    }
}
