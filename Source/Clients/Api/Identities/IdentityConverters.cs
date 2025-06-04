// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Identities;

/// <summary>
/// Converts between contracts and API models for identities.
/// </summary>
internal static class IdentityConverters
{
    /// <summary>
    /// Converts a contract identity to an API identity.
    /// </summary>
    /// <param name="identity"><see cref="Contracts.Identities.Identity"/> to convert.</param>
    /// <returns>Converted <see cref="Identity"/>.</returns>
    public static Identity ToApi(this Contracts.Identities.Identity identity) => new(
            identity.Subject,
            identity.Name,
            identity.UserName,
            identity.OnBehalfOf?.ToApi());

    /// <summary>
    /// Converts a collection of contract identities to API identities.
    /// </summary>
    /// <param name="identities">Collection of <see cref="Contracts.Identities.Identity"/> to convert.</param>
    /// <returns>Converted collection of <see cref="Identity"/>.</returns>
    public static IEnumerable<Identity> ToApi(this IEnumerable<Contracts.Identities.Identity> identities) =>
        identities.Select(i => i.ToApi()).ToArray();

    /// <summary>
    /// Converts an API identity to a contract identity.
    /// </summary>
    /// <param name="identity"><see cref="Identity"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Identities.Identity"/>.</returns>
    public static Contracts.Identities.Identity ToContract(this Identity identity) => new()
    {
        Subject = identity.Subject,
        Name = identity.Name,
        UserName = identity.UserName,
        OnBehalfOf = identity.OnBehalfOf?.ToContract()
    };
}
