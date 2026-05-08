// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Setup.Execution;

/// <summary>
/// Default implementation of <see cref="ICorrelationIdAccessor"/> that will first attempt to get the correlation id from the request context, and if not found, will fallback to the provided <see cref="Cratis.Execution.CorrelationIdAccessor"/>.
/// </summary>
/// <param name="correlationIdAccessor">The fallback correlation id accessor.</param>
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
