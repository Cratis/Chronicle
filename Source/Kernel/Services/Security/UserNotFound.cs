// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when a user is not found.
/// </summary>
/// <param name="userId">The user ID that was not found.</param>
public class UserNotFound(Guid userId) : Exception($"User with ID '{userId}' was not found");
