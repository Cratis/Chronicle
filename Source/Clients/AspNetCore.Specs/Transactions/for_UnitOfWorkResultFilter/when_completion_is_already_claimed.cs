// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_completion_is_already_claimed : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new() { ClaimFirst = true };

    async Task Because() => await _harness.Run();

    [Fact] void should_leave_the_result_unchanged() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_not_complete_before_the_result_runs() => _harness.CompletedBeforeResult.ShouldBeFalse();
    [Fact] void should_not_commit_behind_the_owner() => _harness.Request.AppendCalls.ShouldEqual(0);
}
