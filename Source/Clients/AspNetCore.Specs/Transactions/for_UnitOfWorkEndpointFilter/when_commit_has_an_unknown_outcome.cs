// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkEndpointFilter;

public class when_commit_has_an_unknown_outcome : Specification
{
    readonly UnitOfWorkEndpointHarness _harness = new();

    void Establish() => _harness.Request.AppendThrows(new InvalidOperationException("append failed"));
    async Task Because() => await _harness.Run();

    [Fact] void should_return_internal_server_error() => ((ProblemHttpResult)_harness.Result!).StatusCode.ShouldEqual(500);
    [Fact] void should_not_retry_the_append() => _harness.Request.AppendCalls.ShouldEqual(1);
}
