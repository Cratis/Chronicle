// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>
/// Completes the unclaimed request unit before returning the endpoint result.
/// </summary>
internal sealed class UnitOfWorkEndpointFilter : IEndpointFilter
{
    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var ownership = context.HttpContext.Features.Get<IUnitOfWorkCompletionOwnershipFeature>();
        if (ownership is null || !ownership.TryClaim(out var claim))
        {
            return await next(context);
        }

        if (claim.UnitOfWork.IsCompleted) return await next(context);

        object? response;
        try
        {
            response = await next(context);
        }
        catch
        {
            await claim.Rollback();
            throw;
        }

        var result = await claim.Commit();
        return result.IsSuccess ? response : UnitOfWorkFailureDetails.ToEndpointResult(result);
    }
}
