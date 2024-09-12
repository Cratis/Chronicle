// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IAsyncActionFilter"/> for managing units of work and errors.
/// </summary>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
public class UnitOfWorkActionFilter(IUnitOfWorkManager unitOfWorkManager, ILogger<UnitOfWorkActionFilter> logger) : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        var unitOfWork = unitOfWorkManager.Current;

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
            foreach (var violation in unitOfWork.GetConstraintViolations())
            {
                var memberName = violation.Details.TryGetValue(WellKnownConstraintDetailKeys.PropertyName, out var propertyName) ? propertyName : string.Empty;
                context.ModelState.AddModelError(memberName, violation.Message);
            }
        }
    }
}