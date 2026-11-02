// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_an_exception_filter_handles_the_exception : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new() { Exception = new InvalidOperationException("action failed") };

    async Task Because() => await _harness.Run();

    [Fact] void should_leave_the_error_result_unchanged() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_roll_back_before_the_result_runs() => _harness.CompletedBeforeResult.ShouldBeTrue();
    [Fact] void should_not_append() => _harness.Request.AppendCalls.ShouldEqual(0);
}
