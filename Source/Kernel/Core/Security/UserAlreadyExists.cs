// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception thrown when the username is already registered.
/// </summary>
/// <param name="username">The duplicate username.</param>
public class UserAlreadyExists(Username username) : Exception($"A user with username '{username}' already exists.");
