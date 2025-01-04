// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Extension methods for converting to and from <see cref="Identity"/>.
/// </summary>
internal static class IdentityConverters
{
    /// <summary>
    /// Convert to <see cref="Contracts.Identities.Identity"/>.
    /// </summary>
    /// <param name="identity"><see cref="Identity"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Identities.Identity"/>.</returns>
    internal static Contracts.Identities.Identity ToContract(this Identity identity) => new()
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
    internal static Identity ToClient(this Contracts.Identities.Identity identity) =>
        new(identity.Subject, identity.Name, identity.UserName, identity.OnBehalfOf?.ToClient());
}
