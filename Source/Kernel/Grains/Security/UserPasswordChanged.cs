// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Security;

/// <summary>
/// Represents the event for a user password that has been changed.
/// </summary>
/// <param name="UserId">The unique identifier for the user.</param>
/// <param name="PasswordHash">The new hashed password.</param>
[EventType]
public record UserPasswordChanged(string UserId, string PasswordHash);
