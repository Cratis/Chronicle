// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Records whether the action pipeline completed without a short-circuit or handled exception.
/// </summary>
/// <param name="options">The Chronicle ASP.NET Core options.</param>
internal sealed class UnitOfWorkActionFilter(IOptions<ChronicleAspNetCoreOptions> options) : IAsyncActionFilter, IOrderedFilter
{
    static readonly object _completedNormally = new();

    /// <inheritdoc/>
    public int Order => int.MinValue;

    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!options.Value.UnitOfWork.ReportFailuresBeforeResponseStarts)
        {
            await next();
            return;
        }

        context.HttpContext.Items[_completedNormally] = false;
        var executed = await next();
        context.HttpContext.Items[_completedNormally] = executed is { Canceled: false, Exception: null, ExceptionHandled: false };
    }

    /// <summary>
    /// Gets whether the action pipeline completed normally in the request.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <returns>Whether completion may commit the unit.</returns>
    internal static bool CompletedNormally(HttpContext context) => context.Items.TryGetValue(_completedNormally, out var completed) && completed is true;
}
