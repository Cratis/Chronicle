// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Identities;

/// <summary>
/// Defines the contract for working with identities.
/// </summary>
[Service]
public interface IIdentities
{
    /// <summary>
    /// Get all identities.
    /// </summary>
    /// <param name="request">The <see cref="GetIdentitiesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A collection of <see cref="Identity"/>.</returns>
    [Operation]
    Task<IEnumerable<Identity>> GetIdentities(GetIdentitiesRequest request, CallContext context = default);

    /// <summary>
    /// Observe all identities.
    /// </summary>
    /// <param name="request">The <see cref="GetIdentitiesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An observable of a collection of <see cref="Identity"/>.</returns>
    [Operation]
    IObservable<IEnumerable<Identity>> ObserveIdentities(GetIdentitiesRequest request, CallContext context = default);
}
