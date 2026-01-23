// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Event that represents a user logging in.
/// </summary>
/// <param name="UserId">The user identifier.</param>
[EventType]
public record UserLoggedIn(UserId UserId);
