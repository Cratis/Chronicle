// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Converts between <see cref="Identity"/> and its contract and storage representations.
/// </summary>
internal static class IdentityRecordConverters
{
    /// <summary>
    /// Converts an API identity to a contract identity.
    /// </summary>
    /// <param name="identity"><see cref="Identity"/> to convert.</param>
    /// <returns>Converted <see cref="Contracts.Sequences.Identity"/>.</returns>
    public static Contracts.Sequences.Identity ToContract(this Identity identity) => new()
    {
        Subject = identity.Subject,
        Name = identity.Name,
        UserName = identity.UserName,
        OnBehalfOf = identity.OnBehalfOf?.ToContract()
    };

    /// <summary>
    /// Converts a storage identity to an API identity.
    /// </summary>
    /// <param name="identity"><see cref="Concepts.Identities.Identity"/> to convert.</param>
    /// <returns>Converted <see cref="Identity"/>.</returns>
    public static Identity ToApi(this Concepts.Identities.Identity identity) => new(
            identity.Subject,
            identity.Name,
            identity.UserName,
            identity.OnBehalfOf?.ToApi());
}
