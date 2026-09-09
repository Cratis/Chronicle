// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception thrown when the initial password event could not be appended.
/// </summary>
/// <param name="userId">The administrator identifier.</param>
public class InitialPasswordCouldNotBeSet(UserId userId) : Exception($"Initial password setup could not be completed for user '{userId}'.");
