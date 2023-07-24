// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Defines a factory that can create <see cref="IMongoDBSinkCollections"/> instances.
/// </summary>
public interface IMongoDBSinkCollectionsFactory
{
    /// <summary>
    /// Creates a new <see cref="IMongoDBSinkCollections"/> for the given <see cref="MicroserviceId"/>, <see cref="TenantId"/> and <see cref="Model"/>.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> to create for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> to create for.</param>
    /// <param name="model"><see cref="Model"/> to create for.</param>
    /// <returns>A <see cref="IMongoDBSinkCollections"/> instance.</returns>
    IMongoDBSinkCollections CreateFor(MicroserviceId microserviceId, TenantId tenantId, Model model);
}
