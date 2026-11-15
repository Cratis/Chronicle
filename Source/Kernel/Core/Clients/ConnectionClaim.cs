// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clients;

/// <summary>
/// Represents the outcome of claiming a connection identifier.
/// </summary>
public enum ConnectionClaim
{
    /// <summary>
    /// The connection identifier is now held by the owner.
    /// </summary>
    Claimed = 0,

    /// <summary>
    /// The connection identifier is not available to the owner.
    /// </summary>
    Refused = 1
}
