// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Represents a filter for managing correlation IDs for outgoing calls.
/// </summary>
/// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> to use.</param>
public class CorrelationIdOutgoingCallFilter(ICorrelationIdAccessor correlationIdAccessor) : IOutgoingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IOutgoingGrainCallContext context)
    {
        if (context.InterfaceName.StartsWith("Cratis"))
        {
            RequestContext.Set(WellKnownKeys.CorrelationId, correlationIdAccessor.Current.Value);
        }
        await context.Invoke();
    }
}
