// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkEndpointFilter;

public class when_commit_conflicts : Specification
{
    readonly UnitOfWorkEndpointHarness _harness = new();

    void Establish() => _harness.Request.AppendReturns(AppendManyResult.Failed(_harness.Request.CorrelationId, [new ConcurrencyViolation("source", 4, 5)]));
    async Task Because() => await _harness.Run();

    [Fact] void should_return_conflict() => ((ProblemHttpResult)_harness.Result!).StatusCode.ShouldEqual(409);
    [Fact] void should_include_the_correlation_id() => ((ProblemHttpResult)_harness.Result!).ProblemDetails.Extensions["correlationId"].ShouldEqual(_harness.Request.CorrelationId.ToString());
}
