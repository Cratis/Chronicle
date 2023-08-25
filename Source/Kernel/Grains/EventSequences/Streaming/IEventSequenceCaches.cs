// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Defines a system that works with event sequence caches.
/// </summary>
public interface IEventSequenceCaches
{
    /// <summary>
    /// Get the cache for a specific event sequence for a specific tenant and microservice.
    /// </summary>
    /// <param name="microserviceId">MicroserviceId to get for.</param>
    /// <param name="tenantId">TenantId to get for.</param>
    /// <param name="eventSequenceId">EventSequenceId to get for.</param>
    /// <returns>The <see cref="IEventSequenceCache"/> associated.</returns>
    IEventSequenceCache GetFor(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId);

    /// <summary>
    /// Prime all caches.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task PrimeAll();

    /// <summary>
    /// Check if any of the caches are under pressure.
    /// </summary>
    /// <returns>True if any is, false if not.</returns>
    bool IsUnderPressure();

    /// <summary>
    /// Purge items from any of the caches that are under pressure.
    /// </summary>
    void Purge();
}
