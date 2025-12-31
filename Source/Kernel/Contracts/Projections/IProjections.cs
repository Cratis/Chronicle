// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Projections;

/// <summary>
/// Defines the contract for working with projections.
/// </summary>
[Service]
public interface IProjections
{
    /// <summary>
    /// Register projections.
    /// </summary>
    /// <param name="request">The <see cref="RegisterRequest"/> holding all registrations.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Register(RegisterRequest request, CallContext context = default);

    /// <summary>
    /// Get an instance by a specific models key and projection id.
    /// </summary>
    /// <param name="request"><see cref="GetInstanceByIdRequest"/> holding the details.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="ProjectionResult"/> containing the projected result and details of the projection.</returns>
    [Operation]
    Task<ProjectionResult> GetInstanceById(GetInstanceByIdRequest request, CallContext context = default);

    /// <summary>
    /// Gets an instance by a specific model key and projection id for a specific session.
    /// </summary>
    /// <param name="request"><see cref="GetInstanceByIdForSessionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="ProjectionResult"/> containing the projected result and details of the projection.</returns>
    [Operation]
    Task<ProjectionResult> GetInstanceByIdForSession(GetInstanceByIdForSessionRequest request, CallContext context = default);

    /// <summary>
    /// Gets an instance by a specific model key and projection id for a specific session with events applied.
    /// </summary>
    /// <param name="request"><see cref="GetInstanceByIdForSessionWithEventsAppliedRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="ProjectionResult"/> containing the projected result and details of the projection.</returns>
    [Operation]
    Task<ProjectionResult> GetInstanceByIdForSessionWithEventsApplied(GetInstanceByIdForSessionWithEventsAppliedRequest request, CallContext context = default);

    /// <summary>
    /// Observe changes for a specific projection.
    /// </summary>
    /// <param name="request"><see cref="ProjectionWatchRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Observable of <see cref="ProjectionChangeset"/> containing the changeset for the projection.</returns>
    [Operation]
    IObservable<ProjectionChangeset> Watch(ProjectionWatchRequest request, CallContext context = default);

    /// <summary>
    /// Dehydrate a specific projection session.
    /// </summary>
    /// <param name="request"><see cref="DehydrateSessionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default);

    /// <summary>
    /// Get snapshots of a projection grouped by CorrelationId.
    /// </summary>
    /// <param name="request"><see cref="GetSnapshotsByIdRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="GetSnapshotsByIdResponse"/> containing the snapshots.</returns>
    [Operation]
    Task<GetSnapshotsByIdResponse> GetSnapshotsById(GetSnapshotsByIdRequest request, CallContext context = default);

    /// <summary>
    /// Get all projection definitions.
    /// </summary>
    /// <param name="request"><see cref="GetAllDefinitionsRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    Task<IEnumerable<ProjectionDefinition>> GetAllDefinitions(GetAllDefinitionsRequest request, CallContext context = default);

    /// <summary>
    /// Get all projection DSLs.
    /// </summary>
    /// <param name="request"><see cref="GetAllDefinitionsRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="ProjectionWithDsl"/>.</returns>
    Task<IEnumerable<ProjectionWithDsl>> GetAllDsls(GetAllDslsRequest request, CallContext context = default);

    /// <summary>
    /// Preview a projection from its DSL representation.
    /// </summary>
    /// <param name="request"><see cref="PreviewProjectionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="ProjectionPreview"/>.</returns>
    Task<ProjectionPreview> PreviewFromDsl(PreviewProjectionRequest request, CallContext context = default);
}
