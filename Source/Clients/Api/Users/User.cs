// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the read model for a user.
/// </summary>
/// <param name="Id">The user identifier.</param>
/// <param name="Username">The username.</param>
/// <param name="PasswordHash">The hashed password.</param>
/// <param name="HasLoggedIn">Whether the user has logged in.</param>
/// <param name="PasswordChangeRequired">Whether a password change is required.</param>
/// <param name="__lastHandledEventSequenceNumber">The last handled event sequence number.</param>
public record User(
    EventSourceId Id,
    string Username,
    string PasswordHash,
    bool HasLoggedIn,
    bool PasswordChangeRequired,
    EventSequenceNumber __lastHandledEventSequenceNumber);
