// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents an OAuth application.
/// </summary>
public record Application
{
    /// <summary>
    /// Gets or sets the unique identifier for the application.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed client secret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the display name of the application.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the client type (e.g., confidential, public).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the consent type (e.g., explicit, implicit).
    /// </summary>
    public string? ConsentType { get; set; }

    /// <summary>
    /// Gets or sets the permissions granted to the application.
    /// </summary>
    public ImmutableArray<string> Permissions { get; set; } = [];

    /// <summary>
    /// Gets or sets the requirements for the application.
    /// </summary>
    public ImmutableArray<string> Requirements { get; set; } = [];

    /// <summary>
    /// Gets or sets the redirect URIs.
    /// </summary>
    public ImmutableArray<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets the post-logout redirect URIs.
    /// </summary>
    public ImmutableArray<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
    public ImmutableDictionary<string, JsonElement> Properties { get; set; } = [];
}
