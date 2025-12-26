// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Extension methods for converting between contract and API user types.
/// </summary>
public static class UserConverters
{
    /// <summary>
    /// Convert from contract user to API user.
    /// </summary>
    /// <param name="user">Contract user.</param>
    /// <returns>API user.</returns>
    public static User ToApi(this Contracts.Security.User user) => new(
        user.Id,
        user.Username,
        user.Email,
        user.IsActive,
        user.CreatedAt,
        user.LastModifiedAt);

    /// <summary>
    /// Convert from multiple contract users to API users.
    /// </summary>
    /// <param name="users">Contract users.</param>
    /// <returns>API users.</returns>
    public static IEnumerable<User> ToApi(this IEnumerable<Contracts.Security.User> users) =>
        users.Select(ToApi);
}
