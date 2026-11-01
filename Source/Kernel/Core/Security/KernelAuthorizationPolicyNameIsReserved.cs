// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// The exception that is thrown when a host registers an authorization policy named like one owned by the kernel without the kernel's requirement.
/// </summary>
/// <param name="policy">The reserved policy name.</param>
public sealed class KernelAuthorizationPolicyNameIsReserved(string policy)
    : Exception($"The authorization policy '{policy}' is reserved for the kernel and must contain its administrator requirement.");
