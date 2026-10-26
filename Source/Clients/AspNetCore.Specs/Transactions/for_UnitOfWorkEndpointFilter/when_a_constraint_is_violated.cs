// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Cratis.Chronicle.AspNetCore.Transactions.for_UnitOfWorkEndpointFilter;

public class when_a_constraint_is_violated : Specification
{
    readonly UnitOfWorkEndpointHarness _harness = new();

    void Establish() => _harness.Request.AppendReturns(AppendManyResult.Failed(_harness.Request.CorrelationId,
        [new ConstraintViolation("event", 0, ConstraintType.Unique, "unique-name", "Already used", new())]));
    async Task Because() => await _harness.Run();

    [Fact] void should_return_bad_request() => ((ProblemHttpResult)_harness.Result!).StatusCode.ShouldEqual(400);
    [Fact] void should_include_the_validation_errors() => ((HttpValidationProblemDetails)((ProblemHttpResult)_harness.Result!).ProblemDetails).Errors["unique-name"].Single().ShouldEqual("Already used");
    [Fact] void should_include_the_correlation_id() => ((ProblemHttpResult)_harness.Result!).ProblemDetails.Extensions["correlationId"].ShouldEqual(_harness.Request.CorrelationId.ToString());
}
