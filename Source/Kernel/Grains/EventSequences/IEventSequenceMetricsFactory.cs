// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Defines a system that can create instances of <see cref="IEventSequenceMetrics"/>.
/// </summary>
public interface IEventSequenceMetricsFactory
{
    /// <summary>
    /// Create an instance of <see cref="IEventSequenceMetrics"/>.
    /// </summary>
    /// <param name="eventSequenceId">Event sequence to create for.</param>
    /// <param name="microserviceId">Microservice to create for.</param>
    /// <param name="tenantId">Tenant to create for.</param>
    /// <returns><see cref="IEventSequenceMetrics"/> instance.</returns>
    IEventSequenceMetrics CreateFor(EventSequenceId eventSequenceId, MicroserviceId microserviceId, TenantId tenantId);
}
