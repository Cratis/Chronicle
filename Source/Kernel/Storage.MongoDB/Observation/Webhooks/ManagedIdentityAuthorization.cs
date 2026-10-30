// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation.Webhooks;

/// <summary>
/// Represents stored managed identity authorization without a secret.
/// </summary>
public class ManagedIdentityAuthorization
{
    /// <summary>
    /// Gets or sets the client identifier, empty for the system-assigned identity.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requested scope.
    /// </summary>
    public string Scope { get; set; } = string.Empty;
}
