// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// The exception that is thrown when the initial password event could not be appended.
/// </summary>
/// <param name="userId">The administrator user identifier.</param>
public class InitialPasswordCouldNotBeSet(Guid userId) :
    Exception($"Initial password setup could not be completed for user '{userId}'.");
