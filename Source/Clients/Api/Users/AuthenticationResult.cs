// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the result of an authentication attempt.
/// </summary>
/// <param name="Success">Whether the authentication was successful.</param>
/// <param name="UserId">The user identifier if successful.</param>
/// <param name="Error">The error message if unsuccessful.</param>
public record AuthenticationResult(bool Success, UserId? UserId, string? Error)
{
    /// <summary>
    /// Gets or sets a value indicating whether a password change is required.
    /// </summary>
    public bool PasswordChangeRequired { get; init; }
}
