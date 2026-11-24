// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Security;

/// <summary>
/// Represents authorization using a managed or workload identity without a stored secret.
/// </summary>
/// <param name="ClientId">The client identifier, or empty for the system-assigned identity.</param>
/// <param name="Scope">The scope requested for the receiver.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Concepts.Security.ManagedIdentityAuthorization")]
public record ManagedIdentityAuthorization([Id(0)] ManagedIdentityClientId ClientId, [Id(1)] OAuthScope Scope);
