// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Configuration;

namespace Aksio.Cratis.Kernel.Contracts.Events;

/// <summary>
/// Defines the service contract for working with event types.
/// </summary>
[Service]
public interface IEventTypes
{
    /// <summary>
    /// Register a collection of event types.
    /// </summary>
    /// <param name="request">The <see cref="RegisterEventTypesRequest"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Register(RegisterEventTypesRequest request);
}
