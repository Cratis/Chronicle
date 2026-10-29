// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Defines the password requirements shared by Chronicle's password entry points.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>
    /// The minimum length for newly chosen passwords.
    /// </summary>
    public const int MinimumLength = 8;
}
