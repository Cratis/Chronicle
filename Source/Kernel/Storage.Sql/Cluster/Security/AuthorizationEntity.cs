// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents the EF entity for an OAuth authorization.
/// </summary>
public class AuthorizationEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the authorization.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    public Guid? ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the authorization type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the authorization status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the scopes serialized as a JSON array of strings.
    /// </summary>
    public string? Scopes { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTimeOffset? CreationDate { get; set; }

    /// <summary>
    /// Gets or sets additional properties serialized as a JSON string.
    /// </summary>
    public string? Properties { get; set; }
}
