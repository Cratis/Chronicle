// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Completes the unclaimed request unit of work before MVC executes its result when failure reporting is enabled.
/// </summary>
/// <param name="options">The Chronicle ASP.NET Core options.</param>
/// <remarks>
/// Registered globally by <c language="csharp">AddUnitOfWork</c>. Result filters with a lower order run before this filter.
/// A controller or action result filter with the same order (<see cref="CompletionOrder"/>) runs after this filter.
/// Short-circuited actions and handled exceptions roll back instead of committing.
/// </remarks>
public sealed class UnitOfWorkResultFilter(IOptions<ChronicleAspNetCoreOptions> options) : IAsyncAlwaysRunResultFilter, IOrderedFilter
{
    /// <summary>
    /// The maximum result-filter order, placing completion after filters with lower orders, but before
    /// controller or action result filters with this same order.
    /// </summary>
    public const int CompletionOrder = int.MaxValue;

    /// <inheritdoc/>
    public int Order => CompletionOrder;

    /// <inheritdoc/>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (options.Value.UnitOfWork.ReportFailuresBeforeResponseStarts)
        {
            var ownership = context.HttpContext.Features.Get<IUnitOfWorkCompletionOwnershipFeature>();
            if (ownership is not null && ownership.TryClaim(out var claim))
            {
                await Complete(context, claim);
            }
        }

        await next();
    }

    static async Task Complete(ResultExecutingContext context, IUnitOfWorkCompletionClaim claim)
    {
        if (claim.UnitOfWork.IsCompleted) return;
        if (!UnitOfWorkActionFilter.CompletedNormally(context.HttpContext))
        {
            await claim.Rollback();
            return;
        }

        var result = await claim.Commit();
        if (result.IsSuccess) return;
        var problem = UnitOfWorkFailureDetails.Create(result);
        context.Result = new ObjectResult(problem) { StatusCode = problem.Status, ContentTypes = { "application/problem+json" } };
    }
}
