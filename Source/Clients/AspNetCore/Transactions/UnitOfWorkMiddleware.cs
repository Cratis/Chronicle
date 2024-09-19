// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Represents a middleware for managing units of work.
/// </summary>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
/// <param name="next">The next middleware.</param>
public class UnitOfWorkMiddleware(IUnitOfWorkManager unitOfWorkManager, RequestDelegate next)
{
    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>. </param>
    /// <returns>Awaitable task.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var unitOfWork = unitOfWorkManager.Begin(CorrelationId.New());
        try
        {
            await next(context);
        }
        catch
        {
            if (!unitOfWork.IsCompleted)
            {
                await unitOfWork.DisposeAsync();
            }
            throw;
        }
    }
}
