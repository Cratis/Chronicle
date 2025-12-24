// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for adding a user.
/// </summary>
/// <param name="UserId">The user's unique identifier.</param>
/// <param name="Username">The user's username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
public record AddUser(
    string UserId,
    string Username,
    string? Email,
    string Password);
