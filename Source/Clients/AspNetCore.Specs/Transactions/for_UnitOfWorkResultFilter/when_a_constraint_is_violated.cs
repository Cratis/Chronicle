// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkResultFilter;

public class when_a_constraint_is_violated : Specification
{
    readonly UnitOfWorkMvcHarness _harness = new();
    ObjectResult _result;

    void Establish() => _harness.Request.AppendReturns(AppendManyResult.Failed(_harness.Request.CorrelationId,
        [new ConstraintViolation("event", 0, ConstraintType.Unique, "unique-name", "Already used", new())]));

    async Task Because()
    {
        await _harness.Run();
        _result = (ObjectResult)_harness.Result!;
    }

    [Fact] void should_return_bad_request() => _result.StatusCode.ShouldEqual(400);
    [Fact] void should_include_validation_errors() => ((ValidationProblemDetails)_result.Value!).Errors["unique-name"].Single().ShouldEqual("Already used");
    [Fact] void should_include_the_correlation_id() => ((ProblemDetails)_result.Value!).Extensions["correlationId"].ShouldEqual(_harness.Request.CorrelationId.ToString());
}
