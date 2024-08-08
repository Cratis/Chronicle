// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Reflection;

namespace Cratis.Chronicle.Orleans.Transactions;

/// <summary>
/// Represents a filter for managing units of work for incoming calls.
/// </summary>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
public class UnitOfWorkIncomingCallFilter(IUnitOfWorkManager unitOfWorkManager) : IIncomingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        var correlationId = RequestContext.Get(Constants.CorrelationIdKey) as CorrelationId;
        if (correlationId is not null &&
            (context.InterfaceMethod.DeclaringType?.HasInterface<IAggregateRoot>() ?? false) &&
            unitOfWorkManager.TryGetFor(correlationId, out var unitOfWork))
        {
            unitOfWorkManager.SetCurrent(unitOfWork);
        }

        await context.Invoke();
    }
}
