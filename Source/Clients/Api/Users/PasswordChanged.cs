// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Event that represents a user's password being changed.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="PasswordHash">The hashed password.</param>
[EventType]
public record PasswordChanged(UserId UserId, string PasswordHash);
