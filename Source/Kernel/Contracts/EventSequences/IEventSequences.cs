// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Defines the contract for working with event sequences.
/// </summary>
[Service]
public interface IEventSequences
{
    /// <summary>
    /// Append an event to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="AppendResponse"/>.</returns>
    [Operation]
    Task<AppendResponse> Append(AppendRequest request, CallContext context = default);

    /// <summary>
    /// Append many events to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendManyRequest"/> with all the details and events.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="AppendManyResponse"/>.</returns>
    Task<AppendManyResponse> AppendMany(AppendManyRequest request, CallContext context = default);
}
