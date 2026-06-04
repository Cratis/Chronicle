// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.ReadModels;

/// <summary>
/// Defines the contract for working with materialized read model instances.
/// </summary>
[Service]
public interface IMaterializedReadModels
{
    /// <summary>
    /// Get paginated instances of a read model from the sink.
    /// </summary>
    /// <param name="request">The <see cref="GetInstancesRequest"/> for the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="GetInstancesResponse"/> with the instances.</returns>
    [Operation]
    Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, CallContext context = default);

    /// <summary>
    /// Observe changes to paginated instances of a read model.
    /// </summary>
    /// <param name="request">The <see cref="ObserveInstancesRequest"/> for the request.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable stream of instance collections.</returns>
    [Operation]
    IObservable<ObserveInstancesResponse> ObserveInstances(ObserveInstancesRequest request, CallContext context = default);
}
