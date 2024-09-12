// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Chronicle.Orleans.Transactions;

/// <summary>
/// Represents a filter for managing units of work for outgoing calls.
/// </summary>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
public class UnitOfWorkOutgoingCallFilter(IUnitOfWorkManager unitOfWorkManager) : IOutgoingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        var correlationId = unitOfWorkManager.HasCurrent ? unitOfWorkManager.Current.CorrelationId : CorrelationId.New();
        RequestContext.Set(Constants.CorrelationIdKey, correlationId);
        if (context.IsMessageToAggregateRoot() && !unitOfWorkManager.HasCurrent)
        {
            unitOfWorkManager.Begin(correlationId);
        }
        await context.Invoke();
    }
}
