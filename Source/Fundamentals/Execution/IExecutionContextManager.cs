// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Execution;

/// <summary>
/// Defines a system for working with <see cref="ExecutionContext"/>.
/// </summary>
public interface IExecutionContextManager
{
    /// <summary>
    /// Gets whether or not we're in context.
    /// </summary>
    bool IsInContext { get; }

    /// <summary>
    /// Get the current <see cref="ExecutionContext"/> for the current call path.
    /// </summary>
    ExecutionContext Current { get; }

    /// <summary>
    /// Establish an <see cref="ExecutionContext"/> for current call path.
    /// </summary>
    /// <param name="tenantId"><see cref="TenantId"/> to establish for.</param>
    /// <param name="correlationId"><see cref="CorrelationId"/> to establish for.</param>
    /// <param name="microserviceId">Optional <see cref="MicroserviceId"/> to establish for Falls back to the global if not explicitly used.</param>
    /// <returns>Established <see cref="ExecutionContext"/>.</returns>
    ExecutionContext Establish(TenantId tenantId, CorrelationId correlationId, MicroserviceId? microserviceId = default);

    /// <summary>
    /// Set a <see cref="ExecutionContext"/> for current call path.
    /// </summary>
    /// <param name="context"><see cref="ExecutionContext"/> to set.</param>
    void Set(ExecutionContext context);
}
