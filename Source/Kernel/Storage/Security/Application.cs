// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Security;
using ApplicationId = Cratis.Chronicle.Concepts.Security.ApplicationId;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents an OAuth application.
/// </summary>
public record Application
{
    /// <summary>
    /// Gets or sets the unique identifier for the application.
    /// </summary>
    public ApplicationId Id { get; set; } = ApplicationId.NotSet;

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public ClientId ClientId { get; set; } = new ClientId(string.Empty);

    /// <summary>
    /// Gets or sets the hashed client secret.
    /// </summary>
    public ClientSecret? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the display name of the application.
    /// </summary>
    public ApplicationDisplayName? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the client type (e.g., confidential, public).
    /// </summary>
    public ApplicationType? Type { get; set; }

    /// <summary>
    /// Gets or sets the consent type (e.g., explicit, implicit).
    /// </summary>
    public ConsentType? ConsentType { get; set; }

    /// <summary>
    /// Gets or sets the permissions granted to the application.
    /// </summary>
    public ImmutableArray<Permission> Permissions { get; set; } = [];

    /// <summary>
    /// Gets or sets the requirements for the application.
    /// </summary>
    public ImmutableArray<Requirement> Requirements { get; set; } = [];

    /// <summary>
    /// Gets or sets the redirect URIs.
    /// </summary>
    public ImmutableArray<RedirectUri> RedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets the post-logout redirect URIs.
    /// </summary>
    public ImmutableArray<RedirectUri> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
#pragma warning disable IDE0301 // Simplify collection initialization - due to net8 and net9 not having a default constructor for this
    public ImmutableDictionary<PropertyName, JsonElement> Properties { get; set; } = ImmutableDictionary<PropertyName, JsonElement>.Empty;
#pragma warning restore IDE0301 // Simplify collection initialization
}
