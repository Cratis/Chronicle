// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_detached_read_guards_a_typed_source : given.source_identity
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_use_the_detached_guard_for_the_same_wire_source(int kind)
    {
        var read = await _store.GetDecisionReads().GetDetached<BookStock>((ReadModelKey)"book");
        var result = await _store.EventLog.AppendMany([new(Source(kind), new StockChanged(0))], guardedBy: [read]);
        Assert.True(result.IsSuccess, string.Join(Environment.NewLine, result.Errors.Select(error => error.Value)));
    }
}
