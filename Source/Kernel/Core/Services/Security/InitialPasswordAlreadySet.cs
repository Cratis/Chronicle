// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when initial-password setup has already completed.
/// </summary>
/// <param name="userId">The administrator user identifier.</param>
public class InitialPasswordAlreadySet(Guid userId) :
    Exception($"Initial password setup has already completed for user '{userId}'.");
