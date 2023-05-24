// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Defines a system that manages <see cref="IEventSequenceCache"/> instances.
/// </summary>
public interface IEventSequenceCacheFactory
{
    /// <summary>
    /// Create a new <see cref="IEventSequenceCache"/> for the given <see cref="MicroserviceId"/>, <see cref="TenantId"/> and <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to create for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to create for.</param>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to create for.</param>
    /// <returns>A new <see cref="IEventSequenceCache"/>.</returns>
    IEventSequenceCache Create(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId);
}
