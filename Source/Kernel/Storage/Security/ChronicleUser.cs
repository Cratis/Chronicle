// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents a user in the Chronicle system.
/// </summary>
/// <param name="Id">The unique identifier for the user.</param>
/// <param name="Username">The username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="PasswordHash">The hashed password.</param>
/// <param name="SecurityStamp">A random value that should change whenever user credentials change.</param>
/// <param name="IsActive">Whether the user account is active.</param>
/// <param name="CreatedAt">When the user was created.</param>
/// <param name="LastModifiedAt">When the user was last modified.</param>
public record ChronicleUser(
    string Id,
    string Username,
    string? Email,
    string? PasswordHash,
    string? SecurityStamp,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt);
