// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the current identity details for a link in an event's caused-by chain.
/// </summary>
/// <param name="Subject">The identity subject, empty if it could not be resolved.</param>
/// <param name="Name">The display name, only present for a resolved identity.</param>
/// <param name="UserName">The identity username, empty if it could not be resolved.</param>
/// <param name="Resolution">The resolution outcome.</param>
/// <param name="OnBehalfOf">The next link in the chain.</param>
public record ResolvedIdentity(string Subject, string? Name, string UserName, IdentityResolution Resolution, ResolvedIdentity? OnBehalfOf)
{
    /// <summary>
    /// Converts the recursive identity chain to its wire representation.
    /// </summary>
    /// <returns>The contract identity chain.</returns>
    public Contracts.Sequences.ResolvedIdentity ToContract() => new()
    {
        Subject = Subject,
        Name = Name,
        UserName = UserName,
        Resolution = (Contracts.Sequences.IdentityResolution)Resolution,
        OnBehalfOf = OnBehalfOf?.ToContract()
    };
}
