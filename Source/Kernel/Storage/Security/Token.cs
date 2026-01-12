// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Chronicle.Storage.Security;

/// <summary>
/// Represents an OAuth token.
/// </summary>
public record Token
{
    /// <summary>
    /// Gets or sets the unique identifier for the token.
    /// </summary>
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
    /// Gets or sets the subject (user identifier).
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
    /// Gets or sets when the token was created.
    /// </summary>
    public DateTimeOffset? CreationDate { get; set; }

    /// <summary>
    /// Gets or sets when the token expires.
    /// </summary>
    public DateTimeOffset? ExpirationDate { get; set; }

    /// <summary>
    /// Gets or sets when the token was redeemed.
    /// </summary>
    public DateTimeOffset? RedemptionDate { get; set; }

    /// <summary>
    /// Gets or sets additional properties.
    /// </summary>
#pragma warning disable IDE0301 // Simplify collection initialization - due to net8 and net9 not having a default constructor for this
    public ImmutableDictionary<string, JsonElement> Properties { get; set; } = ImmutableDictionary<string, JsonElement>.Empty;
#pragma warning restore IDE0301 // Simplify collection initialization
}
