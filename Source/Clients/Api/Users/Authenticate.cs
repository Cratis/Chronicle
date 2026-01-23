// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Users;

/// <summary>
/// Represents the command for authenticating a user.
/// </summary>
/// <param name="Username">The username.</param>
/// <param name="Password">The password.</param>
public record Authenticate(string Username, string Password);
