// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Event that represents the initial Admin user being added.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Username">The username.</param>
[EventType]
public record InitialAdminUserAdded(UserId UserId, string Username);
