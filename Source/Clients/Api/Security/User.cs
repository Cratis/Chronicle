// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents a user in the Chronicle system.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="IsActive">Indicates whether the user is active.</param>
/// <param name="CreatedAt">The date and time when the user was created.</param>
/// <param name="LastModifiedAt">The date and time when the user was last modified.</param>
[ReadModel]
public record User(
    Guid Id,
    string Username,
    string? Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt)
{
    /// <summary>
    /// Observes all users.
    /// </summary>
    /// <param name="users">The <see cref="IUsers"/> contract.</param>
    /// <returns>An observable for observing a collection of users.</returns>
    internal static ISubject<IEnumerable<User>> AllUsers(IUsers users) =>
        users.InvokeAndWrapWithTransformSubject(
            token => users.ObserveAll(token),
            response => response.Users.ToApi());
}
