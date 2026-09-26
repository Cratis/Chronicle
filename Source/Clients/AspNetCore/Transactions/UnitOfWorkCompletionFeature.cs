// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.AspNetCore.Transactions;

/// <summary>Request-scoped completion handle owned by the middleware.</summary>
/// <param name="unitOfWork">The request unit of work.</param>
/// <param name="owner">The middleware's owner capability, if supported.</param>
internal sealed class UnitOfWorkCompletionFeature(IUnitOfWork unitOfWork, DecisionReadCommitOwner? owner) : IUnitOfWorkCompletionFeature
{
    /// <inheritdoc/>
    public async Task<IUnitOfWork> CommitAsync()
    {
        if (owner is not null)
        {
            await ((UnitOfWork)unitOfWork).CommitAsOwner(owner);
        }
        else
        {
            await unitOfWork.Commit();
        }
        return unitOfWork;
    }
}
