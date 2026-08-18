// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Converts stored users into the user read model.
/// </summary>
/// <remarks>
/// These live beside the read model rather than on it because a static method on a <c>[ReadModel]</c> whose return
/// shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility is not
/// what the proxy generator looks at. A conversion helper is not an operation anybody should be able to call.
/// </remarks>
internal static class UserConverters
{
    /// <summary>
    /// Converts a stored user into the read model.
    /// </summary>
    /// <param name="user">The stored user.</param>
    /// <returns>The user read model.</returns>
    internal static User ToUser(Storage.Security.User user) =>
        new(
            (Guid)user.Id,
            user.Username,
            user.Email is null ? null : (string)user.Email,
            user.IsActive,
            user.CreatedAt,
            user.LastModifiedAt,
            user.HasLoggedIn);
}
