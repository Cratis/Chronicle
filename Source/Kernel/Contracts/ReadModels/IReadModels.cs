// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Defines the contract for working with read models.
/// </summary>
[Service]
public interface IReadModels
{
    /// <summary>
    /// Register many read models.
    /// </summary>
    /// <param name="request">The <see cref="RegisterManyRequest"/> holding all registrations.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task RegisterMany(RegisterManyRequest request, CallContext context = default);

    /// <summary>
    /// Register a single read model.
    /// </summary>
    /// <param name="request">The <see cref="RegisterSingleRequest"/> holding the registration.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task RegisterSingle(RegisterSingleRequest request, CallContext context = default);

    /// <summary>
    /// Update a read model definition.
    /// </summary>
    /// <param name="request">The <see cref="UpdateDefinitionRequest"/> holding the update.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task UpdateDefinition(UpdateDefinitionRequest request, CallContext context = default);

    /// <summary>
    /// Get all read model definitions.
    /// </summary>
    /// <param name="request">The <see cref="GetDefinitionsRequest"/> for the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="GetDefinitionsResponse"/> with all definitions.</returns>
    [Operation]
    Task<GetDefinitionsResponse> GetDefinitions(GetDefinitionsRequest request, CallContext context = default);

    /// <summary>
    /// Get all occurrences for a specific read model.
    /// </summary>
    /// <param name="request">The <see cref="GetOccurrencesRequest"/> for the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="GetOccurrencesResponse"/> with all occurrences.</returns>
    [Operation]
    Task<GetOccurrencesResponse> GetOccurrences(GetOccurrencesRequest request, CallContext context = default);

    /// <summary>
    /// Get instances of a read model.
    /// </summary>
    /// <param name="request">The <see cref="GetInstancesRequest"/> for the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="GetInstancesResponse"/> with the instances.</returns>
    [Operation]
    Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, CallContext context = default);

    /// <summary>
    /// Get snapshots of a read model grouped by CorrelationId.
    /// </summary>
    /// <param name="request">The <see cref="GetSnapshotsByKeyRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="GetSnapshotsByKeyResponse"/> containing the snapshots.</returns>
    [Operation]
    Task<GetSnapshotsByKeyResponse> GetSnapshotsByKey(GetSnapshotsByKeyRequest request, CallContext context = default);

    /// <summary>
    /// Get a read model instance by its key.
    /// </summary>
    /// <param name="request">The <see cref="GetInstanceByKeyRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="GetInstanceByKeyResponse"/> containing the read model instance.</returns>
    [Operation]
    Task<GetInstanceByKeyResponse> GetInstanceByKey(GetInstanceByKeyRequest request, CallContext context = default);

    /// <summary>
    /// Get all instances of a read model by processing events without pagination.
    /// </summary>
    /// <param name="request">The <see cref="GetAllInstancesRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="GetAllInstancesResponse"/> containing all read model instances.</returns>
    [Operation]
    Task<GetAllInstancesResponse> GetAllInstances(GetAllInstancesRequest request, CallContext context = default);

    /// <summary>
    /// Watch for changes to a read model.
    /// </summary>
    /// <param name="request">The <see cref="WatchRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable stream of <see cref="ReadModelChangeset"/>.</returns>
    [Operation]
    IObservable<ReadModelChangeset> Watch(WatchRequest request, CallContext context = default);

    /// <summary>
    /// Dehydrate a read model session.
    /// </summary>
    /// <param name="request">The <see cref="DehydrateSessionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default);
}
