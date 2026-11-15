// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Webhooks;

/// <summary>
/// Represents a stored webhook signing key.
/// </summary>
public class WebhookSigningKey
{
    /// <summary>
    /// Gets or sets the unique identifier of the key.
    /// </summary>
    [Key]
    [MaxLength(64)]
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="WebhookSigningAlgorithm"/> of the key.
    /// </summary>
    public int Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="WebhookSigningKeyStatus"/> of the key.
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// Gets or sets the encoded public key.
    /// </summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encrypted private material, empty once retired.
    /// </summary>
    public string EncryptedSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the webhook the key is scoped to, or null for a store-wide key.
    /// </summary>
    public string? Webhook { get; set; }

    /// <summary>
    /// Gets or sets when the key was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the overlap window ends.
    /// </summary>
    public DateTimeOffset? OverlapEndsAt { get; set; }

    /// <summary>
    /// Gets or sets when the key was retired.
    /// </summary>
    public DateTimeOffset? RetiredAt { get; set; }
}
