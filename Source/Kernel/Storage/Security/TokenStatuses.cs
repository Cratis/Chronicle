// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Holds the well-known values for <see cref="Token.Status"/>.
/// </summary>
/// <remarks>
/// The values mirror OpenIddict's <c>OpenIddictConstants.Statuses</c> so the storage layer can reason
/// about a token's lifecycle (for example when pruning) without taking a dependency on OpenIddict.
/// </remarks>
public static class TokenStatuses
{
    /// <summary>
    /// The status for a token that is still valid.
    /// </summary>
    public const string Valid = "valid";
}
