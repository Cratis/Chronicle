// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception thrown when initial setup targets someone other than the configured administrator.
/// </summary>
/// <param name="userId">The rejected user identifier.</param>
public class InitialPasswordCanOnlyBeSetForAdministrator(UserId userId) : Exception($"Initial password setup is not allowed for user '{userId}'.");
