// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception thrown when a user could not be appended.
/// </summary>
/// <param name="userId">The user identifier.</param>
public class UserCouldNotBeAdded(UserId userId) : Exception($"The user '{userId}' could not be added.");
