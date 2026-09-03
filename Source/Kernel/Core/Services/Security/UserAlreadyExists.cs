// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when a user with the same username already exists.
/// </summary>
/// <param name="username">The duplicate username.</param>
public class UserAlreadyExists(string username) : Exception($"A user with username '{username}' already exists.");
