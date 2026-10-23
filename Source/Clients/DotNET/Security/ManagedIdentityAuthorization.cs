// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Security;

/// <summary>
/// Represents authorization using a managed or workload identity without a stored secret.
/// </summary>
/// <param name="Scope">The scope requested for the receiver.</param>
/// <param name="ClientId">The client identifier, or null for the system-assigned identity.</param>
public record ManagedIdentityAuthorization(string Scope, string? ClientId = null);
