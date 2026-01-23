// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the command for changing a user's password.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="NewPassword">The new password.</param>
public record ChangePassword(UserId UserId, string NewPassword);
