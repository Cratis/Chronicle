// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Represents an implementation of <see cref="ICorrelationIdAccessor"/> that reads from Orleans request context.
/// </summary>
/// <param name="correlationIdAccessor">The underlying <see cref="Cratis.Execution.CorrelationIdAccessor"/> to fall back to.</param>
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
