// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkEndpointFilter;

public class when_completion_is_already_claimed : Specification
{
    readonly UnitOfWorkEndpointHarness _harness = new() { ClaimFirst = true };

    async Task Because() => await _harness.Run();

    [Fact] void should_preserve_the_result() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_leave_the_unit_to_its_owner() => _harness.CompletedBeforeResult.ShouldBeFalse();
    [Fact] void should_not_append() => _harness.Request.AppendCalls.ShouldEqual(0);
}
