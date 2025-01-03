// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Services.Identities;

/// <summary>
/// Converters for converting to/from contracts for identities.
/// </summary>
public static class IdentityConverters
{
    /// <summary>
    /// Convert from contract to concept.
    /// </summary>
    /// <param name="identities">Identities to convert.</param>
    /// <returns>Converted Identities.</returns>
    public static IEnumerable<Identity> ToContract(this IEnumerable<Concepts.Identities.Identity> identities) =>
        identities.Select(_ => _.ToContract());

    /// <summary>
    /// Convert from contract to concept.
    /// </summary>
    /// <param name="identity">Identity to convert.</param>
    /// <returns>Converted Identity.</returns>
    public static Identity ToContract(this Concepts.Identities.Identity identity) => new()
    {
        Subject = identity.Subject,
        Name = identity.Name,
        UserName = identity.UserName,
        OnBehalfOf = identity.OnBehalfOf?.ToContract()
    };
}
