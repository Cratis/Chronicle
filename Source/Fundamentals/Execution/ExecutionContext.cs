// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Execution
{
    /// <summary>
    /// Represents the current context of execution.
    /// </summary>
    /// <param name="TenantId">The <see cref="TenantId"/>.</param>
    /// <param name="CorrelationId">The <see cref="CorrelationId"/>.</param>
    /// <param name="CausationId">The <see cref="CausationId"/>.</param>
    public record ExecutionContext(TenantId TenantId, CorrelationId CorrelationId, CausationId CausationId);
}