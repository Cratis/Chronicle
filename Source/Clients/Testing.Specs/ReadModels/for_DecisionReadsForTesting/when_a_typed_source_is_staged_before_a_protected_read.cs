// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_typed_source_is_staged_before_a_protected_read : given.source_identity
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_refuse_the_read_of_the_same_wire_source(int kind)
    {
        using var frame = Begin();
        Stage(Source(kind));
        var error = await Catch.Exception(() => _store.GetDecisionReads().GetFor<BookStock>(_unit, (ReadModelKey)"book"));
        error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
        _unit.GetAppendConditions().ShouldBeEmpty();
    }
}
