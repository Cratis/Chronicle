// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Seeding;

/// <summary>
/// Defines the contract for event seeding operations.
/// </summary>
[Service]
public interface IEventSeeding
{
    /// <summary>
    /// Seed events into the event store.
    /// </summary>
    /// <param name="request">The <see cref="SeedRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Seed(SeedRequest request, CallContext context = default);

    /// <summary>
    /// Get global seed data for an event store.
    /// </summary>
    /// <param name="request">The request containing the event store name.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Collection of global seed data entries.</returns>
    [Operation]
    Task<SeedDataResponse> GetGlobalSeedData(GetSeedDataRequest request, CallContext context = default);

    /// <summary>
    /// Get namespace-specific seed data.
    /// </summary>
    /// <param name="request">The request containing the event store and namespace names.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Collection of namespace-specific seed data entries.</returns>
    [Operation]
    Task<SeedDataResponse> GetNamespaceSeedData(GetSeedDataRequest request, CallContext context = default);
}
