// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Represents a filter for managing correlation IDs for incoming calls.
/// </summary>
public class CorrelationIdIncomingCallFilter : IIncomingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (RequestContext.Get(WellKnownKeys.CorrelationId) is Guid correlationId)
        {
            Cratis.Execution.CorrelationIdAccessor.SetCurrent(correlationId);
        }

        await context.Invoke();
    }
}
