// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_reporting_is_disabled_for_a_page_handler : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new() { ActionRan = false, PageRan = true };

    void Establish() => _harness.Options.UnitOfWork.ReportFailuresBeforeResponseStarts = false;
    async Task Because() => await _harness.Run();

    [Fact] void should_leave_the_result_unchanged() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_not_claim_completion() => _harness.ClaimedBeforeResult.ShouldBeFalse();
    [Fact] void should_leave_completion_to_middleware() => _harness.Request.AppendCalls.ShouldEqual(1);
}
