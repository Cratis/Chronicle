// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Represents a middleware for managing units of work.
/// </summary>
/// <param name="next">The next middleware.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
public class UnitOfWorkMiddleware(RequestDelegate next, ILogger<UnitOfWorkMiddleware> logger)
{
    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>. </param>
    /// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
    /// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> to get the <see cref="CorrelationId"/> from.</param>
    /// <returns>Awaitable task.</returns>
    public async Task InvokeAsync(HttpContext context, IUnitOfWorkManager unitOfWorkManager, ICorrelationIdAccessor correlationIdAccessor)
    {
        var correlationId = correlationIdAccessor.Current;
        if (correlationId == CorrelationId.NotSet)
        {
            correlationId = CorrelationId.New();
            CorrelationIdAccessor.SetCurrent(correlationId);
        }
        var unitOfWork = unitOfWorkManager.Begin(correlationId);
        var owner = (unitOfWork as UnitOfWork)?.ClaimDecisionReadCommitOwnership();
        var previousFeature = context.Features.Get<IUnitOfWorkCompletionFeature>();
        var previousOwnership = context.Features.Get<IUnitOfWorkCompletionOwnershipFeature>();
        var feature = new UnitOfWorkCompletionFeature(unitOfWork, owner);
        context.Features.Set<IUnitOfWorkCompletionFeature>(feature);
        context.Features.Set<IUnitOfWorkCompletionOwnershipFeature>(feature);
        try
        {
            await next(context);

            if (feature.CurrentClaim is { } claim)
            {
                await EnsureClaimCompleted(claim);
                return;
            }

            // Handle unit of work completion and constraint violations
            if (!unitOfWork.IsCompleted)
            {
                if (owner is not null)
                {
                    await ((UnitOfWork)unitOfWork).CommitAsOwner(owner);
                }
                else
                {
                    await unitOfWork.Commit();
                }

                if (!unitOfWork.IsSuccess)
                {
                    await ReportFailure(context, unitOfWork);
                }
            }
            else if (!feature.HasCompleted)
            {
                logger.AlreadyCompletedManually(unitOfWork.CorrelationId);
            }
        }
        catch
        {
            if (feature.CurrentClaim is { } failedClaim)
            {
                await EnsureClaimCompleted(failedClaim);
            }
            else if (!unitOfWork.IsCompleted)
            {
                if (owner is not null)
                {
                    try
                    {
                        await ((UnitOfWork)unitOfWork).RollbackAsOwner(owner);
                    }
                    catch (UnitOfWorkIsCompleting)
                    {
                        // An early commit is still in flight. No rollback occurred; preserve the action's exception.
                    }
                    catch (UnitOfWorkIsAlreadyCommitted)
                    {
                        // The early commit finished between the completion check and the rollback attempt.
                    }
                    catch (UnitOfWorkIsAlreadyRolledBack)
                    {
                        // Public rollback or disposal completed the plain unit after the completion check.
                    }
                }
                else
                {
                    unitOfWork.Dispose();
                }
            }
            throw;
        }
        finally
        {
            context.Features.Set(previousFeature);
            context.Features.Set(previousOwnership);
        }
    }

    async Task EnsureClaimCompleted(IUnitOfWorkCompletionClaim claim)
    {
        if (claim.Result is not null) return;

        // The claim owner never completed the unit. Never commit behind its back.
        logger.ClaimedButNotCompleted(claim.UnitOfWork.CorrelationId);
        await claim.Rollback();
    }

    async Task ReportFailure(HttpContext context, IUnitOfWork unitOfWork)
    {
        var concurrencyViolations = unitOfWork.GetConcurrencyViolations().ToArray();
        var decisionConflicts = unitOfWork.GetDecisionConflicts().ToArray();
        var constraintViolations = unitOfWork.GetConstraintViolations().ToArray();
        var appendErrors = unitOfWork.GetAppendErrors().ToArray();
        logger.CommitFailedAfterResponse(
            unitOfWork.CorrelationId,
            context.Response.HasStarted,
            concurrencyViolations.Length,
            decisionConflicts.Length,
            constraintViolations.Length,
            appendErrors.Length);

        var options = context.RequestServices?.GetService<IOptions<ChronicleAspNetCoreOptions>>()?.Value;
        if (options?.UnitOfWork.ReportFailuresBeforeResponseStarts != true || context.Response.HasStarted) return;

        var (status, title) = (concurrencyViolations.Length + decisionConflicts.Length, constraintViolations.Length) switch
        {
            (> 0, _) => (StatusCodes.Status409Conflict, "The request conflicts with a concurrent change."),
            (_, > 0) => (StatusCodes.Status400BadRequest, "The request violates a constraint."),
            _ => (StatusCodes.Status500InternalServerError, "The request could not be committed.")
        };
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["correlationId"] = unitOfWork.CorrelationId.ToString();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
