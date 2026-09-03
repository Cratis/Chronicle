// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when initial-password setup targets a user other than the configured administrator.
/// </summary>
/// <param name="userId">The user identifier that was rejected.</param>
public class InitialPasswordCanOnlyBeSetForAdministrator(Guid userId) :
    Exception($"Initial password setup is not allowed for user '{userId}'.");
