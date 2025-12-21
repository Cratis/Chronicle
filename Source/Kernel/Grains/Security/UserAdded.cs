// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for a user that has been added.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <param name="Username">The username.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="PasswordHash">The hashed password.</param>
[EventType]
public record UserAdded(string UserId, string Username, string? Email, string PasswordHash);
