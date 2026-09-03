// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when a password-change event could not be appended.
/// </summary>
/// <param name="userId">The user identifier.</param>
public class PasswordCouldNotBeChanged(Guid userId) :
    Exception($"The password could not be changed for user '{userId}'.");
