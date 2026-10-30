// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents authorization using a managed or workload identity without a stored secret.
/// </summary>
/// <param name="ClientId">The client identifier, or empty for the system-assigned identity.</param>
/// <param name="Scope">The scope requested for the receiver.</param>
public record ManagedIdentityAuthorization(ManagedIdentityClientId ClientId, OAuthScope Scope);
