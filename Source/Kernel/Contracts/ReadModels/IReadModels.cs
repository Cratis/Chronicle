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
    /// Register read models.
    /// </summary>
    /// <param name="request">The <see cref="RegisterRequest"/> holding all registrations.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Register(RegisterRequest request, CallContext context = default);
}
