// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Services.Identities;

/// <summary>
/// Converters for converting to/from contracts for identities.
/// </summary>
internal static class IdentityConverters
{
    /// <summary>
    /// Convert from contract to concept.
    /// </summary>
    /// <param name="identities">Identities to convert.</param>
    /// <returns>Converted Identities.</returns>
    public static IEnumerable<Contracts.Identities.Identity> ToContract(this IEnumerable<Identity> identities) =>
        identities.Select(_ => _.ToContract());

    /// <summary>
    /// Convert from contract to concept.
    /// </summary>
    /// <param name="identity">Identity to convert.</param>
    /// <returns>Converted Identity.</returns>
    public static Contracts.Identities.Identity ToContract(this Identity identity) => new()
    {
        Subject = identity.Subject,
        Name = identity.Name,
        UserName = identity.UserName,
        OnBehalfOf = identity.OnBehalfOf?.ToContract()
    };

    /// <summary>
    /// Convert to Chronicle representation.
    /// </summary>
    /// <param name="identity"><see cref="Contracts.Identities.Identity"/> to convert from.</param>
    /// <returns>Converted <see cref="Identity"/>.</returns>
    public static Identity ToChronicle(this Contracts.Identities.Identity identity) =>
        new(identity.Subject, identity.Name, identity.UserName, identity.OnBehalfOf?.ToChronicle());
}
