// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        try
        {
            await next(context);

            // Handle unit of work completion and constraint violations
            if (!unitOfWork.IsCompleted)
            {
                await unitOfWork.Commit();
            }
            else
            {
                logger.AlreadyCompletedManually(unitOfWork.CorrelationId);
            }

            if (!unitOfWork.IsSuccess)
            {
                // Try to add model errors if this is an MVC context
                var actionContext = context.Features.Get<ActionContext>();
                if (actionContext is not null)
                {
                    foreach (var violation in unitOfWork.GetConstraintViolations())
                    {
                        var memberName = violation.Details.TryGetValue(WellKnownConstraintDetailKeys.PropertyName, out var propertyName) ? propertyName : string.Empty;
                        actionContext.ModelState.AddModelError(memberName, violation.Message);
                    }
                }
            }
        }
        catch
        {
            if (!unitOfWork.IsCompleted)
            {
                unitOfWork.Dispose();
            }
            throw;
        }
    }
}
