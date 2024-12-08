// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Jobs;

/// <summary>
/// Defines the contract work working with the jobs system.
/// </summary>
[Service]
public interface IJobs
{
    /// <summary>
    /// Get all jobs.
    /// </summary>
    /// <param name="request">The <see cref="GetAllRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Collection of all jobs.</returns>
    Task<IEnumerable<Job>> GetAll(GetAllRequest request, CallContext context = default);
}
