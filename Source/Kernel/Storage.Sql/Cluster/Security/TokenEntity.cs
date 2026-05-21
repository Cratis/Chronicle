// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security;

/// <summary>
/// Represents the EF entity for an OAuth token.
/// </summary>
public class TokenEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the token.
    /// </summary>
    [Key]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the authorization identifier.
    /// </summary>
    public string? AuthorizationId { get; set; }

    /// <summary>
    /// Gets or sets the subject.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the token type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the token status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the token payload.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Gets or sets the reference identifier.
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    public DateTimeOffset? CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets the redemption date.
    /// </summary>
    public DateTimeOffset? RedemptionDate { get; set; }

    /// <summary>
    /// Gets or sets additional properties serialized as a JSON string.
    /// </summary>
    public string? Properties { get; set; }
}
