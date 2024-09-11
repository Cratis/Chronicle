// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Represents an implementation of <see cref="IAsyncActionFilter"/> for managing units of work and errors.
/// </summary>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
public class UnitOfWorkActionFilter(IUnitOfWorkManager unitOfWorkManager) : IAsyncActionFilter
{
    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await next();

        var unitOfWork = unitOfWorkManager.Current;

        // TODO: Create Issue: unitOfWork.Commit here throws an exception if it's already commited, which maybe is a bit strict. Consider maybe to not throw in Commit()?
        await unitOfWork.Commit();
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
