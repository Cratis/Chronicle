// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Maps completion failures to the shared HTTP problem-details contract.
/// </summary>
internal static class UnitOfWorkFailureDetails
{
    /// <summary>
    /// Creates problem details for a completion failure.
    /// </summary>
    /// <param name="result">The failed completion result.</param>
    /// <returns>The HTTP problem details.</returns>
    internal static ProblemDetails Create(UnitOfWorkCompletionResult result)
    {
        var problem = result switch
        {
            { HasConflicts: true } => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The request conflicts with a concurrent change."
            },
            { HasConstraintViolations: true } => ValidationProblem(result),
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "The request could not be committed."
            }
        };
        problem.Extensions["correlationId"] = result.CorrelationId.ToString();
        return problem;
    }

    /// <summary>
    /// Creates a minimal API result for a completion failure.
    /// </summary>
    /// <param name="result">The failed completion result.</param>
    /// <returns>The HTTP problem result.</returns>
    internal static IResult ToEndpointResult(UnitOfWorkCompletionResult result)
    {
        var problem = Create(result);
        return problem is ValidationProblemDetails validation
            ? Results.ValidationProblem(validation.Errors, statusCode: validation.Status, title: validation.Title, extensions: validation.Extensions)
            : Results.Problem(problem);
    }

    static ValidationProblemDetails ValidationProblem(UnitOfWorkCompletionResult result) => new(result.ConstraintViolations
        .GroupBy(_ => _.ConstraintName.ToString())
        .ToDictionary(_ => _.Key, _ => _.Select(violation => violation.Message.ToString()).ToArray()))
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "The request violates a constraint."
    };
}
