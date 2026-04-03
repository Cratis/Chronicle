// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.EntityFrameworkCore.Json;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents the EF entity for an OAuth application.
/// </summary>
public class ApplicationEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the application.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

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
    /// Gets or sets the application type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the consent type.
    /// </summary>
    public string? ConsentType { get; set; }

    /// <summary>
    /// Gets or sets the permissions as a JSON array of strings.
    /// </summary>
    [Json]
    public IEnumerable<string> Permissions { get; set; } = [];

    /// <summary>
    /// Gets or sets the requirements as a JSON array of strings.
    /// </summary>
    [Json]
    public IEnumerable<string> Requirements { get; set; } = [];

    /// <summary>
    /// Gets or sets the redirect URIs as a JSON array of strings.
    /// </summary>
    [Json]
    public IEnumerable<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets the post-logout redirect URIs as a JSON array of strings.
    /// </summary>
    [Json]
    public IEnumerable<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Gets or sets additional properties serialized as a JSON string.
    /// </summary>
    public string? Properties { get; set; }
}
