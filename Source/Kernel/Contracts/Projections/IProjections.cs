// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

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
    Task<ProjectionResult> GetInstanceById(GetInstanceByIdRequest request, CallContext context = default);

    /// <summary>
    /// Gets an instance by a specific model key and projection id for a specific session.
    /// </summary>
    /// <param name="request"><see cref="GetInstanceByIdForSessionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="ProjectionResult"/> containing the projected result and details of the projection.</returns>
    Task<ProjectionResult> GetInstanceByIdForSession(GetInstanceByIdForSessionRequest request, CallContext context = default);

    /// <summary>
    /// Gets an instance by a specific model key and projection id for a specific session with events applied.
    /// </summary>
    /// <param name="request"><see cref="GetInstanceByIdForSessionWithEventsAppliedRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns><see cref="ProjectionResult"/> containing the projected result and details of the projection.</returns>
    Task<ProjectionResult> GetInstanceByIdFOrSessionWithEventsApplied(GetInstanceByIdForSessionWithEventsAppliedRequest request, CallContext context = default);

    /// <summary>
    /// Dehydrate a specific projection session.
    /// </summary>
    /// <param name="request"><see cref="DehydrateSessionRequest"/> with all the details about the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    Task DehydrateSession(DehydrateSessionRequest request, CallContext context = default);
}
