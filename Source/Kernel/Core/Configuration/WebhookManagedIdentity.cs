// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Operator-approved identities and scopes for webhook token acquisition.
/// </summary>
public class WebhookManagedIdentity
{
    /// <summary>
    /// Gets the allowed scopes, matched exactly ignoring case. An empty list disables managed identity webhooks.
    /// </summary>
    public IEnumerable<string> AllowedScopes { get; init; } = [];

    /// <summary>
    /// Gets the allowed non-empty client ids, matched exactly.
    /// </summary>
    public IEnumerable<string> AllowedClientIds { get; init; } = [];

    /// <summary>
    /// Gets whether an empty client id may use the system-assigned identity or the workload identity environment default.
    /// </summary>
    public bool AllowSystemAssigned { get; init; }
}
