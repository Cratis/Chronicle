// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Represents an Orleans-aware implementation of <see cref="ICorrelationIdAccessor"/> that reads
/// the correlation ID from the current Orleans request context, falling back to the ambient
/// <see cref="Cratis.Execution.CorrelationIdAccessor"/> when no request context is available.
/// </summary>
/// <param name="correlationIdAccessor">The ambient <see cref="Cratis.Execution.CorrelationIdAccessor"/> used as a fallback.</param>
public class CorrelationIdAccessor(Cratis.Execution.CorrelationIdAccessor correlationIdAccessor) : ICorrelationIdAccessor
{
    /// <inheritdoc/>
    public CorrelationId Current
    {
        get
        {
            var value = RequestContext.Get(WellKnownKeys.CorrelationId);
            if (value is Guid guid)
            {
                return new CorrelationId(guid);
            }
            return correlationIdAccessor.Current;
        }
    }
}
