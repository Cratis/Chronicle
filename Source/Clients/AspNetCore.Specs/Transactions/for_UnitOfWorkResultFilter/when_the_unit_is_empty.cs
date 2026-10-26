// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_the_unit_is_empty : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new() { AddEvents = false };

    async Task Because() => await _harness.Run();

    [Fact] void should_leave_the_result_unchanged() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_complete_before_the_result_runs() => _harness.CompletedBeforeResult.ShouldBeTrue();
    [Fact] void should_not_append() => _harness.Request.AppendCalls.ShouldEqual(0);
}
