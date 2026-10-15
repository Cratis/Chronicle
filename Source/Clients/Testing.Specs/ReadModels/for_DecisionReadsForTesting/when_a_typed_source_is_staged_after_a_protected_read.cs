// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_a_typed_source_is_staged_after_a_protected_read : given.source_identity
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task should_commit_with_one_condition_for_the_wire_source(int kind)
    {
        using var frame = Begin();
        var read = await _store.GetDecisionReads().GetFor<BookStock>(_unit, (ReadModelKey)"book");
        read.Instance!.Available.ShouldEqual(1);
        Stage(Source(kind));
        await _unit.CommitAsOwner(_owner);
        Assert.True(_unit.IsSuccess, string.Join(Environment.NewLine, _unit.GetAppendErrors().Select(error => error.Value)));
        _unit.GetAppendConditions().Single().Label.ShouldEqual("book");
    }
}
