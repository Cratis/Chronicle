// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Captures the persisted identity chain in an append receipt.
/// </summary>
/// <param name="Subject">The identity's subject.</param>
/// <param name="Name">The identity's name.</param>
/// <param name="UserName">The identity's username.</param>
/// <param name="OnBehalfOf">The identity on whose behalf the append was made.</param>
public record AppendIdentity(string Subject, string Name, string UserName, AppendIdentity? OnBehalfOf = null)
{
    /// <summary>
    /// Takes a snapshot of the persisted identity chain.
    /// </summary>
    /// <param name="identity">The persisted identity.</param>
    /// <returns>The receipt identity.</returns>
    internal static AppendIdentity From(Concepts.Identities.Identity identity) =>
        new(identity.Subject, identity.Name, identity.UserName, identity.OnBehalfOf is null ? null : From(identity.OnBehalfOf));
}
