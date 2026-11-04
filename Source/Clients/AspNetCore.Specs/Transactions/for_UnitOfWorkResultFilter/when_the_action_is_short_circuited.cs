// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_the_action_is_short_circuited : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new() { Canceled = true };

    async Task Because() => await _harness.Run();

    [Fact] void should_leave_the_result_unchanged() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_roll_back_before_the_result_runs() => _harness.CompletedBeforeResult.ShouldBeTrue();
    [Fact] void should_not_append() => _harness.Request.AppendCalls.ShouldEqual(0);
}
