// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Resolves the principal for a connection operation.
/// </summary>
public interface IConnectionOwnerResolver
{
    /// <summary>
    /// Gets or sets the network principal for the current continuation.
    /// </summary>
    ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// Gets or sets whether the current continuation originated from a network transport.
    /// </summary>
    bool HasTransport { get; set; }

    /// <summary>
    /// Resolves the current continuation's owner.
    /// </summary>
    /// <returns>The owner.</returns>
    ConnectionOwner Current();

    /// <summary>
    /// Resolves a network principal, or the in-process owner when there is no network call.
    /// </summary>
    /// <param name="principal">The network principal, which may be missing.</param>
    /// <param name="hasTransport">Whether a network call exists even when its identity is missing.</param>
    /// <returns>The owner.</returns>
    ConnectionOwner Resolve(ClaimsPrincipal? principal, bool hasTransport = false);
}
