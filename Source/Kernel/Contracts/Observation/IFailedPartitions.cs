// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Observation;

/// <summary>
/// Defines the contract for working with failed partitions.
/// </summary>
[Service]
public interface IFailedPartitions
{
    /// <summary>
    /// Get all failed partitions for an event store and namespace and optionally filter by observer id.
    /// </summary>
    /// <param name="request">The <see cref="GetFailedPartitionsRequest"/>.</param>
    /// /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="FailedPartition"/>.</returns>
    Task<IEnumerable<FailedPartition>> GetFailedPartitions(GetFailedPartitionsRequest request, CallContext context = default);

    /// <summary>
    /// Observe all failed partitions for an event store and namespace and optionally filter by observer id.
    /// </summary>
    /// <param name="request">The <see cref="GetFailedPartitionsRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of a collection of <see cref="FailedPartition"/>.</returns>
    IObservable<IEnumerable<FailedPartition>> ObserveFailedPartitions(GetFailedPartitionsRequest request, CallContext context = default);
}
