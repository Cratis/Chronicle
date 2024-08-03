// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ProtoBuf.Grpc.Configuration;

namespace Cratis.Chronicle.Contracts.Events.Constraints;

/// <summary>
/// Defines the service contract for working with constraints.
/// </summary>
[Service]
public interface IConstraints
{
    /// <summary>
    /// Register a collection of constraints.
    /// </summary>
    /// <param name="request">The <see cref="RegisterConstraintsRequest"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Register(RegisterConstraintsRequest request);
}
