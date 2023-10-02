// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Identities;

/// <summary>
/// Extension methods for converting to and from <see cref="Identity"/>.
/// </summary>
public static class IdentityConverters
{
    /// <summary>
    /// Convert to <see cref="Kernel.Contracts.Identities.Identity"/>.
    /// </summary>
    /// <param name="identity"><see cref="Identity"/> to convert.</param>
    /// <returns>Converted <see cref="Kernel.Contracts.Identities.Identity"/>.</returns>
    public static Kernel.Contracts.Identities.Identity ToContract(this Identity identity) => new()
    {
        Subject = identity.Subject,
        Name = identity.Name,
        UserName = identity.UserName,
        OnBehalfOf = identity.OnBehalfOf?.ToContract()
    };
}
