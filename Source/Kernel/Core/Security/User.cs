// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents the read model for a user account, providing query access to the users system store.
/// </summary>
/// <param name="Id">The unique identifier for the user.</param>
/// <param name="Username">The username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="IsActive">Indicates whether the user account is active.</param>
/// <param name="CreatedAt">When the user account was created.</param>
/// <param name="LastModifiedAt">When the user account was last modified.</param>
/// <param name="HasLoggedIn">Whether the user has ever logged in and set their password.</param>
[ReadModel]
[BelongsTo(WellKnownServices.Users)]
public record User(
    Guid Id,
    string Username,
    string? Email,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt,
    bool HasLoggedIn)
{
    /// <summary>
    /// Observes all users in the system.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> to observe users from.</param>
    /// <returns>An observable subject emitting collections of users.</returns>
    internal static ISubject<IEnumerable<User>> AllUsers(IStorage storage) =>
        storage.System.Users
            .ObserveAll()
            .TransformSubject(users => users.Select(ToUser));

    static User ToUser(Storage.Security.User user) =>
        new(
            (Guid)user.Id,
            user.Username,
            user.Email is null ? null : (string)user.Email,
            user.IsActive,
            user.CreatedAt,
            user.LastModifiedAt,
            user.HasLoggedIn);
}
