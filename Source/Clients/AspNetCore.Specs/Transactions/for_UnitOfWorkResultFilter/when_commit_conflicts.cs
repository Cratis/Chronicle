// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_commit_conflicts : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new();
    ObjectResult _result;

    void Establish() => _harness.Request.AppendReturns(new AppendManyResult
    {
        CorrelationId = _harness.Request.CorrelationId,
        ConcurrencyViolations = [new ConcurrencyViolation("source", 4, 5)],
        ConstraintViolations = [new ConstraintViolation("event", 0, ConstraintType.Unique, "unique-name", "Already used", new())]
    });
    async Task Because()
    {
        await _harness.Run();
        _result = (ObjectResult)_harness.Result!;
    }

    [Fact] void should_prefer_conflict_over_constraint_violations() => _result.StatusCode.ShouldEqual(409);
    [Fact] void should_return_problem_json() => _result.ContentTypes.Single().ShouldEqual("application/problem+json");
    [Fact] void should_include_the_correlation_id() => ((ProblemDetails)_result.Value!).Extensions["correlationId"].ShouldEqual(_harness.Request.CorrelationId.ToString());
    [Fact] void should_complete_before_the_result_runs() => _harness.CompletedBeforeResult.ShouldBeTrue();
}
