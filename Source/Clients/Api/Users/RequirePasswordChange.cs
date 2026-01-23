// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the command for requiring a password change for a user.
/// </summary>
/// <param name="UserId">The user identifier.</param>
public record RequirePasswordChange(UserId UserId);
