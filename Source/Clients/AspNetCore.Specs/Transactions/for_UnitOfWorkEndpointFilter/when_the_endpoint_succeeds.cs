// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkEndpointFilter;

public class when_the_endpoint_succeeds : Specification
{
    readonly UnitOfWorkEndpointHarness _harness = new();

    async Task Because() => await _harness.Run();

    [Fact] void should_preserve_the_endpoint_result() => _harness.Result.ShouldEqual(_harness.Original);
    [Fact] void should_leave_the_unit_open_for_the_handler() => _harness.CompletedInHandler.ShouldBeFalse();
    [Fact] void should_commit_before_returning_the_result() => _harness.CompletedBeforeResult.ShouldBeTrue();
    [Fact] void should_append_once() => _harness.Request.AppendCalls.ShouldEqual(1);
}
